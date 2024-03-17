﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresLibrary;
using EffectLibraryTest;
using ShaderLibrary;

namespace ShaderLibrary.CompileTool
{
    public class TestTOTK
    {
        public static void Run(string bfres_path, string mesh_name, string shader_path)
        {
            var bfsha = new BfshaFile(shader_path);
            ResFile resFile = new ResFile(bfres_path);

            UniformBlockPrinter.Print(bfsha.ShaderModels[0].UniformBlocks.Values.FirstOrDefault(x => x.Type == 1));

            var program_gbuffer = GetShaderProgram(bfsha, resFile, mesh_name, "gsys_assign_gbuffer");
            var program_depth = GetShaderProgram(bfsha, resFile, mesh_name, "gsys_assign_zonly");
            var program_mat = GetShaderProgram(bfsha, resFile, mesh_name, "gsys_assign_material");

            UAMShaderCompiler.Compile(program_gbuffer.VertexShader, "Shader/TOTK/Vertex.vert", "vert");
            UAMShaderCompiler.Compile(program_depth.VertexShader, "Shader/TOTK/Vertex.vert", "vert");
            UAMShaderCompiler.Compile(program_mat.VertexShader, "Shader/TOTK/Vertex.vert", "vert");

            UAMShaderCompiler.Compile(program_gbuffer.FragmentShader, "Shader/TOTK/Pixel.frag", "frag");

            bfsha.Save("shaderNEW.bfsha");
        }

        static BnshFile.BnshShaderProgram GetShaderProgram(BfshaFile bfsha, ResFile resFile, string mesh_name, string pipeline)
        {
            var model = resFile.Models[0];
            var shape = model.Shapes[mesh_name];
            var material = model.Materials[shape.MaterialIndex];
            var shader = bfsha.ShaderModels[material.ShaderAssign.ShadingModelName];

            //get options
            var shader_options = GetOptionSearch(material, shape, pipeline);
            //get program
            var programIdx = shader.GetProgramIndex(shader_options);
            var indices_total = shader.GetProgramIndexList(shader_options);

            Console.WriteLine($"Found index {programIdx} of {indices_total.Count}");

            //get target variation data
            return shader.GetVariation(programIdx).BinaryProgram;
        }

        static Dictionary<string, string> GetOptionSearch(Material material, Shape shape, string pipeline = "gsys_assign_material")
        {
            Dictionary<string, string> options = new Dictionary<string, string>();
            foreach (var op in material.ShaderAssign.ShaderOptions)
            {
                string choice = op.Value.ToString();
                if (op.Value == "True")
                    choice = "1";
                else if (op.Value == "False")
                    choice = "0";
                else if (op.Value == "<Default Value>")
                    continue;

                options.Add(op.Key, choice);
            }

            options.Add("gsys_weight", shape.VertexSkinCount.ToString()); //skin count
            options.Add("gsys_assign_type", pipeline); //material pass

            //render info configures options of compiled shaders (alpha testing and render state)
            var renderMode = material.GetRenderInfoString("gsys_render_state_mode");
            var alphaTest = material.GetRenderInfoString("gsys_alpha_test_enable");

            if (options.ContainsKey("gsys_renderstate"))
                options["gsys_renderstate"] = RenderStateModes[renderMode];

            if (options.ContainsKey("gsys_alpha_test_enable"))
                options["gsys_alpha_test_enable"] = alphaTest == "true" ? "1" : "0";

            return options;
        }

        static Dictionary<string, string> RenderStateModes = new Dictionary<string, string>()
        {
            { "opaque", "0" },
            { "mask", "1" },
            { "translucent", "2" },
            { "custom", "3" },
        };
    }
}
