using SharpDX.D3DCompiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace MiniCast.Client.Effects
{
    public class ColoredQuadEffect : ShaderEffect
    {
        private class CompiledShader
        {
            private static CompiledShader coloredQuadEffect = new CompiledShader();
            private PixelShader pixelShader;

            private CompiledShader()
            {
                var shaderCode = Properties.Resources.ColoredQuadShader;
                var byteCode = ShaderBytecode.Compile(shaderCode, "main", "ps_2_0", ShaderFlags.OptimizationLevel3, sourceFileName: "ColoredQuadBuffer");
                
                MemoryStream byteCodeMs = new MemoryStream(byteCode.Bytecode.Data);
                pixelShader = new PixelShader();
                pixelShader.SetStreamSource(byteCodeMs);
            }

            public static PixelShader Get()
            {
                return coloredQuadEffect.pixelShader;
            }
        }

        public ColoredQuadEffect()
        {
            PixelShader = CompiledShader.Get();
        }
    }
}
