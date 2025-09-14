
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;
using chip8_emulator.Interfaces;
using System.Collections.Generic;

namespace chip8_emulator.IO
{
    public class VeldridRenderer : IRenderer, IInputProvider, IDisposable
    {
        private const string VertexCode = @"
#version 450

layout(location = 0) in vec2 Position;
layout(location = 1) in vec4 Color;

layout(location = 0) out vec4 fsin_Color;

void main()
{
    gl_Position = vec4(Position, 0, 1);
    fsin_Color = Color;
}";

        private const string FragmentCode = @"
#version 450

layout(location = 0) in vec4 fsin_Color;
layout(location = 0) out vec4 fsout_Color;

void main()
{
    fsout_Color = fsin_Color;
}";

        private readonly bool[,] _pixels;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly CommandList _commandList;
        private readonly DeviceBuffer _vertexBuffer;
        
        private readonly Pipeline _pipeline;
        private readonly Sdl2Window _window;
        private readonly Shader[] _shaders;

        public bool IsActive => _window.Exists;

        private readonly Dictionary<Key, int> _keyMap = new Dictionary<Key, int>
        {
            { Key.Number1, 0x1 }, { Key.Number2, 0x2 }, { Key.Number3, 0x3 }, { Key.Number4, 0xC },
            { Key.Q, 0x4 }, { Key.W, 0x5 }, { Key.E, 0x6 }, { Key.R, 0xD },
            { Key.A, 0x7 }, { Key.S, 0x8 }, { Key.D, 0x9 }, { Key.F, 0xE },
            { Key.Z, 0xA }, { Key.X, 0x0 }, { Key.C, 0xB }, { Key.V, 0xF }
        };

        private struct VertexPositionColor
        {
            public const uint SizeInBytes = 24;
            public Vector2 Position;
            public RgbaFloat Color;
            public VertexPositionColor(Vector2 position, RgbaFloat color)
            {
                Position = position;
                Color = color;
            }
        }

        public VeldridRenderer()
        {
            _pixels = new bool[IRenderer.Width, IRenderer.Height];
            _window = new Sdl2Window("CHIP-8 Emulator", 100, 100, IRenderer.Width * 10, IRenderer.Height * 10, SDL_WindowFlags.OpenGL, true);
            _graphicsDevice = VeldridStartup.CreateGraphicsDevice(_window);
            
            ResourceFactory factory = _graphicsDevice.ResourceFactory;

            VertexPositionColor[] quadVertices =
            {
                new VertexPositionColor(new Vector2(-1f, 1f), RgbaFloat.Green),
                new VertexPositionColor(new Vector2(1f, 1f), RgbaFloat.Green),
                new VertexPositionColor(new Vector2(-1f, -1f), RgbaFloat.Green),
                new VertexPositionColor(new Vector2(1f, -1f), RgbaFloat.Green)
            };
            
            BufferDescription vbDescription = new BufferDescription(
                (uint)(VertexPositionColor.SizeInBytes * quadVertices.Length * IRenderer.Width * IRenderer.Height),
                BufferUsage.VertexBuffer | BufferUsage.Dynamic);
            _vertexBuffer = factory.CreateBuffer(vbDescription);

            

            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

            ShaderDescription vertexShaderDesc = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(VertexCode),
                "main");
            ShaderDescription fragmentShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(FragmentCode),
                "main");

            _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);

            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;
            pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: true,
                depthWriteEnabled: true,
                comparisonKind: ComparisonKind.LessEqual);
            pipelineDescription.RasterizerState = new RasterizerStateDescription(
                cullMode: FaceCullMode.Back,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.Clockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false);
            pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleList;
            pipelineDescription.ResourceLayouts = System.Array.Empty<ResourceLayout>();
            pipelineDescription.ShaderSet = new ShaderSetDescription(
                vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
                shaders: _shaders);
            pipelineDescription.Outputs = _graphicsDevice.SwapchainFramebuffer.OutputDescription;

            _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

            _commandList = factory.CreateCommandList();
        }

        public void ProcessInput(Chip8 cpu)
        {
            var snapshot = _window.PumpEvents();
            foreach (var keyEvent in snapshot.KeyEvents)
            {
                if (_keyMap.TryGetValue(keyEvent.Key, out int chip8Key))
                {
                    cpu.SetKey(chip8Key, keyEvent.Down);
                }
            }
        }

        public void Clear()
        {
            for (int x = 0; x < IRenderer.Width; x++)
            {
                for (int y = 0; y < IRenderer.Height; y++)
                {
                    _pixels[x, y] = false;
                }
            }
        }

        public bool GetPixel(int x, int y)
        {
            return _pixels[x, y];
        }

        public void FlipPixel(int x, int y)
        {
            _pixels[x, y] ^= true;
        }

        public void RenderFrame()
        {
            _commandList.Begin();
            _commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
            _commandList.ClearColorTarget(0, RgbaFloat.Black);

            List<VertexPositionColor> vertices = new List<VertexPositionColor>();
            for (int x = 0; x < IRenderer.Width; x++)
            {
                for (int y = 0; y < IRenderer.Height; y++)
                {
                    if (_pixels[x, y])
                    {
                        float xpos = (x / (float)IRenderer.Width) * 2 - 1;
                        float ypos = 1.0f - (y / (float)IRenderer.Height) * 2.0f;
                        float w = 2.0f / IRenderer.Width;
                        float h = 2.0f / IRenderer.Height;

                        vertices.Add(new VertexPositionColor(new Vector2(xpos, ypos + h), RgbaFloat.Green));
                        vertices.Add(new VertexPositionColor(new Vector2(xpos + w, ypos + h), RgbaFloat.Green));
                        vertices.Add(new VertexPositionColor(new Vector2(xpos, ypos), RgbaFloat.Green));

                        vertices.Add(new VertexPositionColor(new Vector2(xpos + w, ypos + h), RgbaFloat.Green));
                        vertices.Add(new VertexPositionColor(new Vector2(xpos + w, ypos), RgbaFloat.Green));
                        vertices.Add(new VertexPositionColor(new Vector2(xpos, ypos), RgbaFloat.Green));
                    }
                }
            }

            if (vertices.Count > 0)
            {
                _graphicsDevice.UpdateBuffer(_vertexBuffer, 0, vertices.ToArray());
                _commandList.SetVertexBuffer(0, _vertexBuffer);
                _commandList.SetPipeline(_pipeline);
                _commandList.Draw((uint)vertices.Count);
            }

            _commandList.End();
            _graphicsDevice.SubmitCommands(_commandList);
            _graphicsDevice.SwapBuffers();
        }

        public void Dispose()
        {
            _pipeline.Dispose();
            foreach (Shader shader in _shaders)
            {
                shader.Dispose();
            }
            _commandList.Dispose();
            _vertexBuffer.Dispose();
            
            _graphicsDevice.Dispose();
            _window.Close();
        }
    }
}
