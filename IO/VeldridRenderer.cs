
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
    /// <summary>
    /// Implementa o renderizador gráfico e o provedor de entrada para o emulador CHIP-8 usando a biblioteca Veldrid.
    /// Também adiciona efeitos de pós-processamento para simular uma tela CRT.
    /// </summary>
    public class VeldridRenderer : IRenderer, IInputProvider, IDisposable
    {
        /// <summary>
        /// Shader de vértice para a passagem de pós-processamento.
        /// Este shader é responsável por transformar as coordenadas do vértice e passar as coordenadas de textura.
        /// Ele desenha um quad que cobre a tela inteira.
        /// </summary>
        private const string PostProcessingVertexShader = @"
#version 450

layout(location = 0) in vec2 Position; // Posição do vértice (NDC)
layout(location = 1) in vec2 TexCoords; // Coordenadas de textura

layout(location = 0) out vec2 fsin_TexCoords; // Coordenadas de textura passadas para o fragment shader

void main()
{
    gl_Position = vec4(Position, 0, 1); // Define a posição final do vértice
    fsin_TexCoords = TexCoords; // Passa as coordenadas de textura
}";

        /// <summary>
        /// Shader de fragmento para a passagem de pós-processamento.
        /// Este shader aplica efeitos CRT (distorção de barril e scanlines) à textura da tela do CHIP-8.
        /// </summary>
        private const string PostProcessingFragmentShader = @"
#version 450

layout(location = 0) in vec2 fsin_TexCoords; // Coordenadas de textura interpoladas do vértice
layout(set = 0, binding = 0) uniform texture2D SourceTexture; // A textura da tela do CHIP-8
layout(set = 0, binding = 1) uniform sampler SourceSampler;   // O sampler para a textura

layout(location = 0) out vec4 fsout_Color; // A cor final do fragmento

// Função para aplicar distorção de barril às coordenadas de textura.
// Isso simula a curvatura de uma tela CRT.
vec2 distort(vec2 p)
{
    float barrel_distortion = 0.075; // Intensidade da distorção
    float r2 = (p.x * p.x + p.y * p.y); // Quadrado da distância do centro
    p *= (1.0 + barrel_distortion * r2); // Aplica a distorção
    return p;
}

void main()
{
    // Aplica a distorção de barril às coordenadas de textura.
    // As coordenadas são convertidas de [0,1] para [-1,1] para o cálculo da distorção
    // e depois de volta para [0,1] para amostragem da textura.
    vec2 distorted_uv = distort(fsin_TexCoords * 2.0 - 1.0) * 0.5 + 0.5;

    // Define a cor do bezel (borda) do monitor
    vec4 bezelColor = vec4(0.0, 0.0, 0.0, 1.0);
    vec4 color = bezelColor;

    // Garante que as coordenadas distorcidas estejam dentro dos limites válidos [0,1].
    // Isso evita amostragem fora da textura e artefatos nas bordas.
    if (distorted_uv.x >= 0.0 && distorted_uv.x <= 1.0 && distorted_uv.y >= 0.0 && distorted_uv.y <= 1.0)
    {
        // Amostra a cor da textura da tela do CHIP-8 usando as coordenadas distorcidas.
        color = texture(sampler2D(SourceTexture, SourceSampler), distorted_uv);
        
        // Aplica o efeito de scanlines (linhas de varredura) apenas na área da tela.
        float scanline = sin(fsin_TexCoords.y * 400.0) * 0.1; // 400.0 controla a frequência, 0.1 a intensidade
        color.rgb -= scanline; // Escurece a cor RGB com base na scanline
    }

    fsout_Color = color; // Define a cor final do pixel
}";
        
        // Matriz booleana que representa o estado dos pixels da tela do CHIP-8 (64x32).
        private readonly bool[,] _pixels;
        // Dispositivo gráfico Veldrid, responsável pela comunicação com a GPU.
        private readonly GraphicsDevice _graphicsDevice;
        // Lista de comandos Veldrid, usada para registrar operações de renderização.
        private readonly CommandList _commandList;
        // Pipeline gráfico Veldrid, que define o estado de renderização (shaders, blend, etc.).
        private readonly Pipeline _pipeline;
        // Janela SDL2, onde a renderização será exibida.
        private readonly Sdl2Window _window;
        // Array de shaders compilados (vértice e fragmento).
        private readonly Shader[] _shaders;

        // Recursos da tela do CHIP-8
        // Textura Veldrid que armazena os pixels da tela do CHIP-8.
        private readonly Texture _chip8Texture;
        // View da textura, usada para ligar a textura a um shader.
        private readonly TextureView _chip8TextureView;
        
        // Recursos de pós-processamento
        // Conjunto de recursos Veldrid, liga a textura e o sampler ao pipeline.
        private readonly ResourceSet _postProcessingResourceSet;
        // Buffer de vértices para o quad que cobre a tela inteira.
        private readonly DeviceBuffer _screenQuadVertexBuffer;
        // Buffer de índices para o quad que cobre a tela inteira.
        private readonly DeviceBuffer _screenQuadIndexBuffer;

        /// <summary>
        /// Indica se a janela do emulador está ativa.
        /// </summary>
        public bool IsActive => _window.Exists;

        /// <summary>
        /// Mapeamento de teclas do teclado físico para as teclas do CHIP-8.
        /// </summary>
        private readonly Dictionary<Key, int> _keyMap = new Dictionary<Key, int>
        {
            { Key.Number1, 0x1 }, { Key.Number2, 0x2 }, { Key.Number3, 0x3 }, { Key.Number4, 0xC },
            { Key.Q, 0x4 }, { Key.W, 0x5 }, { Key.E, 0x6 }, { Key.R, 0xD },
            { Key.A, 0x7 }, { Key.S, 0x8 }, { Key.D, 0x9 }, { Key.F, 0xE },
            { Key.Z, 0xA }, { Key.X, 0x0 }, { Key.C, 0xB }, { Key.V, 0xF }
        };
        
        /// <summary>
        /// Construtor da classe VeldridRenderer.
        /// Inicializa todos os recursos gráficos necessários para renderização e pós-processamento.
        /// </summary>
        public VeldridRenderer()
        {
            // Inicializa a matriz de pixels da tela do CHIP-8.
            _pixels = new bool[IRenderer.Width, IRenderer.Height];
            // Cria e configura a janela SDL2.
            _window = new Sdl2Window("CHIP-8 Emulator", 100, 100, IRenderer.Width * 10, IRenderer.Height * 10, SDL_WindowFlags.OpenGL | SDL_WindowFlags.Resizable, true);
            // Cria o dispositivo gráfico Veldrid.
            _graphicsDevice = VeldridStartup.CreateGraphicsDevice(_window);
            _window.Resized += () => 
            {
                _graphicsDevice.ResizeMainWindow((uint)_window.Width, (uint)_window.Height);
            };
            
            // Obtém a fábrica de recursos do dispositivo gráfico.
            ResourceFactory factory = _graphicsDevice.ResourceFactory;

            // Cria a textura que irá conter os pixels da tela do CHIP-8.
            // Formato R8_G8_B8_A8_UNorm (RGBA de 8 bits por canal, normalizado).
            // Uso Sampled (pode ser amostrada por shaders) e Storage (pode ser escrita).
            _chip8Texture = factory.CreateTexture(TextureDescription.Texture2D(
                IRenderer.Width, IRenderer.Height, 1, 1,
                PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled | TextureUsage.Storage));
            // Cria uma view para a textura, necessária para ligá-la aos shaders.
            _chip8TextureView = factory.CreateTextureView(_chip8Texture);

            // Define os vértices para um quad que cobre a tela inteira (Normalized Device Coordinates - NDC).
            // Cada vértice tem uma posição (vec2) e uma coordenada de textura (vec2).
            Vector2[] quadVertices = 
            {
                // Posição        // Coordenada de Textura
                new Vector2(-1f, 1f), new Vector2(0, 0), // Top-left
                new Vector2(1f, 1f), new Vector2(1, 0),  // Top-right
                new Vector2(-1f, -1f), new Vector2(0, 1), // Bottom-left
                new Vector2(1f, -1f), new Vector2(1, 1)   // Bottom-right
            };
            // Define os índices para desenhar dois triângulos que formam o quad.
            ushort[] quadIndices = { 0, 1, 2, 1, 3, 2 }; // Triângulos: (0,1,2) e (1,3,2)

            // Cria o buffer de vértices na GPU e atualiza com os dados do quad.
            _screenQuadVertexBuffer = factory.CreateBuffer(new BufferDescription((uint)(quadVertices.Length * sizeof(float) * 2), BufferUsage.VertexBuffer));
            _graphicsDevice.UpdateBuffer(_screenQuadVertexBuffer, 0, quadVertices);

            // Cria o buffer de índices na GPU e atualiza com os dados do quad.
            _screenQuadIndexBuffer = factory.CreateBuffer(new BufferDescription((uint)(quadIndices.Length * sizeof(ushort)), BufferUsage.IndexBuffer));
            _graphicsDevice.UpdateBuffer(_screenQuadIndexBuffer, 0, quadIndices);

            // Configuração do pipeline gráfico para a passagem de pós-processamento.
            // Define o layout dos vértices (posição e coordenadas de textura).
            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

            // Cria as descrições dos shaders de vértice e fragmento.
            ShaderDescription vertexShaderDesc = new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(PostProcessingVertexShader), "main");
            ShaderDescription fragmentShaderDesc = new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(PostProcessingFragmentShader), "main");

            // Compila os shaders a partir do código SPIR-V.
            _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);

            // Cria o layout de recursos, que descreve como os recursos (texturas, samplers)
            // serão ligados aos shaders.
            ResourceLayout resourceLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("SourceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SourceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            // Configura a descrição do pipeline gráfico.
            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend; // Configuração de blend padrão.
            pipelineDescription.DepthStencilState = new DepthStencilStateDescription(false, false, ComparisonKind.LessEqual); // Desabilita depth/stencil.
            pipelineDescription.RasterizerState = new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, false); // Configuração de rasterização.
            pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleList; // Desenha triângulos.
            pipelineDescription.ResourceLayouts = new[] { resourceLayout }; // Define o layout de recursos.
            pipelineDescription.ShaderSet = new ShaderSetDescription(new[] { vertexLayout }, _shaders); // Define os shaders e o layout de vértice.
            pipelineDescription.Outputs = _graphicsDevice.SwapchainFramebuffer.OutputDescription; // Define o framebuffer de saída (a janela principal).

            // Cria o pipeline gráfico.
            _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

            // Cria o conjunto de recursos, ligando a textura da tela do CHIP-8 e um sampler
            // ao pipeline de pós-processamento.
            _postProcessingResourceSet = factory.CreateResourceSet(new ResourceSetDescription(resourceLayout, _chip8TextureView, _graphicsDevice.Aniso4xSampler));

            // Cria a lista de comandos, onde as operações de renderização são registradas.
            _commandList = factory.CreateCommandList();
        }

        /// <summary>
        /// Processa a entrada do usuário (teclado) e atualiza o estado da CPU do CHIP-8.
        /// </summary>
        /// <param name="cpu">A instância da CPU do CHIP-8.</param>
        public void ProcessInput(Chip8 cpu)
        {
            // Obtém um snapshot dos eventos de entrada da janela.
            var snapshot = _window.PumpEvents();
            // Itera sobre os eventos de tecla.
            foreach (var keyEvent in snapshot.KeyEvents)
            {
                // Se a tecla pressionada/solta está mapeada para uma tecla do CHIP-8,
                // atualiza o estado da CPU.
                if (_keyMap.TryGetValue(keyEvent.Key, out int chip8Key))
                {
                    cpu.SetKey(chip8Key, keyEvent.Down);
                }
            }
        }

        /// <summary>
        /// Limpa a tela do CHIP-8, definindo todos os pixels como 'false'.
        /// </summary>
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

        /// <summary>
        /// Obtém o estado de um pixel específico na tela do CHIP-8.
        /// </summary>
        /// <param name="x">Coordenada X do pixel.</param>
        /// <param name="y">Coordenada Y do pixel.</param>
        /// <returns>True se o pixel estiver ligado, false caso contrário.</returns>
        public bool GetPixel(int x, int y)
        {
            return _pixels[x, y];
        }

        /// <summary>
        /// Inverte o estado de um pixel específico na tela do CHIP-8.
        /// </summary>
        /// <param name="x">Coordenada X do pixel.</param>
        /// <param name="y">Coordenada Y do pixel.</param>
        public void FlipPixel(int x, int y)
        {
            _pixels[x, y] ^= true; // XOR para inverter o estado
        }

        /// <summary>
        /// Renderiza um único quadro da tela do emulador.
        /// Este método atualiza a textura da tela do CHIP-8 e a renderiza na janela
        /// com os efeitos de pós-processamento aplicados.
        /// </summary>
        public void RenderFrame()
        {
            // 1. Atualiza os dados da textura da tela do CHIP-8 na GPU.
            // Cria um array de uints, onde cada uint representa um pixel RGBA (32 bits).
            uint[] pixelData = new uint[IRenderer.Width * IRenderer.Height];
            // Define as cores para verde (pixel ligado) e preto (pixel desligado) no formato ABGR.
            // 0xFF00FF00 -> Alpha=FF, Blue=00, Green=FF, Red=00 (Verde)
            // 0xFF000000 -> Alpha=FF, Blue=00, Green=00, Red=00 (Preto)
            uint green = 0xFF00FF00; 
            uint black = 0xFF000000; 

            // Preenche o array pixelData com base no estado da matriz _pixels.
            for (int y = 0; y < IRenderer.Height; y++)
            {
                for (int x = 0; x < IRenderer.Width; x++)
                {
                    pixelData[y * IRenderer.Width + x] = _pixels[x, y] ? green : black;
                }
            }
            // Envia os dados atualizados para a textura na GPU.
            _graphicsDevice.UpdateTexture(_chip8Texture, pixelData, 0, 0, 0, IRenderer.Width, IRenderer.Height, 1, 0, 0);

            // Inicia o registro de comandos de renderização.
            _commandList.Begin();

            // 2. Passagem de Pós-processamento: Renderiza a textura da tela do CHIP-8 na janela com efeitos.
            // Define o framebuffer de saída como o framebuffer da janela principal.
            _commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
            // Limpa o alvo de cor do framebuffer para preto.
            _commandList.ClearColorTarget(0, RgbaFloat.Black);
            // Define o pipeline gráfico a ser usado (com os shaders de pós-processamento).
            _commandList.SetPipeline(_pipeline);
            // Liga o buffer de vértices do quad da tela.
            _commandList.SetVertexBuffer(0, _screenQuadVertexBuffer);
            // Liga o buffer de índices do quad da tela.
            _commandList.SetIndexBuffer(_screenQuadIndexBuffer, IndexFormat.UInt16);
            // Liga o conjunto de recursos (textura da tela do CHIP-8 e sampler) ao pipeline.
            _commandList.SetGraphicsResourceSet(0, _postProcessingResourceSet);
            // Desenha o quad indexado (dois triângulos que formam a tela).
            _commandList.DrawIndexed(
                indexCount: 6,          // 6 índices para 2 triângulos
                instanceCount: 1,       // Apenas uma instância
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);

            // Finaliza o registro de comandos e os submete à GPU.
            _commandList.End();
            _graphicsDevice.SubmitCommands(_commandList);
            // Troca os buffers da janela para exibir o quadro renderizado.
            _graphicsDevice.SwapBuffers();
        }

        /// <summary>
        /// Libera todos os recursos gráficos alocados.
        /// É crucial chamar este método para evitar vazamentos de memória da GPU.
        /// </summary>
        public void Dispose()
        {
            // Libera os recursos do pipeline principal.
            _pipeline.Dispose();
            foreach (Shader shader in _shaders)
            {
                shader.Dispose();
            }
            _commandList.Dispose();
            
            // Libera os recursos da tela do CHIP-8 e de pós-processamento.
            _chip8Texture.Dispose();
            _chip8TextureView.Dispose();
            _screenQuadVertexBuffer.Dispose();
            _screenQuadIndexBuffer.Dispose();
            _postProcessingResourceSet.Dispose();

            // Libera o dispositivo gráfico e fecha a janela.
            _graphicsDevice.Dispose();
            _window.Close();
        }
    }
}
