using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomRenderPassFeature_Select : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        public string SelectTag;
        public Material stencilMaterial;
        public RenderTargetIdentifier source;
        public Material Blur;
        public RenderTexture Temp;
        public RenderTargetHandle SourceTemp;
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        private void InitTexture()
        {
            Temp = new RenderTexture(Camera.main.pixelWidth,Camera.main.pixelHeight, 0);
            SourceTemp.Init("_SourceTempTexture");
        }
        public CustomRenderPass()
        {
            InitTexture();
        }
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            source = renderingData.cameraData.renderer.cameraColorTarget;
            CommandBuffer cmd = CommandBufferPool.Get("SelectRenderPass");
            RenderTargetIdentifier temp = new RenderTargetIdentifier(Temp);           
            cmd.GetTemporaryRT(SourceTemp.id, renderingData.cameraData.cameraTargetDescriptor);
            cmd.SetRenderTarget(temp);
            GameObject[] Renderers = GameObject.FindGameObjectsWithTag(SelectTag);
            foreach (GameObject r in Renderers)
            {
                if (r.GetComponent<Renderer>() == null) { continue; }
                cmd.DrawRenderer(r.GetComponent<Renderer>(), stencilMaterial); // 将renderer和material提交到主camera的commandbuffer列表进行渲染
            }
            Blur.SetTexture("_StencilTexture", Temp);
            Blit(cmd, source, SourceTemp.Identifier(),Blur);
            Blit(cmd, SourceTemp.Identifier(), source);
            context.ExecuteCommandBuffer(cmd);            
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            Temp.Release();
        }
    }
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent WhenToInsert = RenderPassEvent.AfterRendering;
        public string SelectFather = "Selecter" ;
        public Material blur = null;

    }
    public Settings settings = new Settings();
    CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();
        m_ScriptablePass.SelectTag = settings.SelectFather;
        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = settings.WhenToInsert;
        m_ScriptablePass.stencilMaterial = new Material(Shader.Find("Custom/Outline/Stencil"));
        m_ScriptablePass.Blur = settings.blur;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


