using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

public class MetaballRenderFeature : ScriptableRendererFeature
{
    class MetaballRenderPass : ScriptableRenderPass
    {
        // used to label this pass in Unity's Frame Debug utility
        private string _profilerTag;

        private Material _vertexColorMaterial;
        private Material _normalsMaterial;
        private Material _metaBallBlitMaterial;
        private Material _finalBlitMaterial;
        
        private RenderTargetIdentifier _cameraColorTargetIdent;

        private RenderTargetIdentifier _vertexColorRTIdentifier;
        private const string VERTEX_COLOR_RT_NAME = "LayerRT";
        private int _vertexColorRTID = 0;

        private RenderTargetIdentifier _normalsRTIdentifier;
        private const string NORMALS_RT_NAME = "NormalsRT";
        private int _normalsRTID = 1;

        private RenderTargetIdentifier _metaballRTIdentifier;
        private const string METABALL_RT_NAME = "MetaballRT";
        private int _metaballRTID = 2;

        private FilteringSettings _filteringSettings;

        private RenderQueueType _renderQueueType;
        
        private List<ShaderTagId> _shaderTagIds;
        private RenderStateBlock _renderStateBlock;

        private int _layerMask;

        public MetaballRenderPass(
            string profilerTag,
            RenderPassEvent renderPassEventArg,
            RenderQueueType renderQueueTypeArg,
            Material vertexColorMaterialArg,
            Material normalsMaterialArg,
            Material metaBallBlitMaterialArg,
            Material finalBlitMaterialArg,
            int layerMaskArg,
            string[] shaderTags)
        {
            _profilerTag = profilerTag;
            renderPassEvent = renderPassEventArg;
            _renderQueueType = renderQueueTypeArg;
            _vertexColorMaterial = vertexColorMaterialArg;
            _normalsMaterial = normalsMaterialArg;
            _metaBallBlitMaterial = metaBallBlitMaterialArg;
            _finalBlitMaterial = finalBlitMaterialArg;
            _layerMask = layerMaskArg;

            RenderQueueRange renderQueueRange = RenderQueueRange.opaque;

            _filteringSettings = new FilteringSettings(renderQueueRange, _layerMask);

            if (shaderTags != null && shaderTags.Length > 0)
            {
                _shaderTagIds = new List<ShaderTagId>(shaderTags.Length);
                for (int i = 0; i < shaderTags.Length; i++)
                { 
                    _shaderTagIds.Add(new ShaderTagId(shaderTags[i]));
                }
            }
            else
            {
                _shaderTagIds = new List<ShaderTagId>(4);
                _shaderTagIds.Add(new ShaderTagId("SRPDefaultUnlit"));
                _shaderTagIds.Add(new ShaderTagId("UniversalForward"));
                _shaderTagIds.Add(new ShaderTagId("UniversalForwardOnly"));
                _shaderTagIds.Add(new ShaderTagId("LightweightForward"));
            }

            _renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            _vertexColorRTID = Shader.PropertyToID(VERTEX_COLOR_RT_NAME);
            _normalsRTID = Shader.PropertyToID(NORMALS_RT_NAME);
            _metaballRTID = Shader.PropertyToID(METABALL_RT_NAME);
        }

        // This isn't part of the ScriptableRenderPass class and is our own addition.
        // For this custom pass we need the camera's color target, so that gets passed in.
        public void Setup(RenderTargetIdentifier cameraColorTargetIdent)
        {
            this._cameraColorTargetIdent = cameraColorTargetIdent;
        }

        // called each frame before Execute, use it to set up things the pass will need
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // create a temporary render texture that matches the camera
                         
            cmd.GetTemporaryRT(_vertexColorRTID, cameraTextureDescriptor);
            cmd.GetTemporaryRT(_normalsRTID, cameraTextureDescriptor);
            cmd.GetTemporaryRT(_metaballRTID, cameraTextureDescriptor);

            _vertexColorRTIdentifier = new RenderTargetIdentifier(_vertexColorRTID);
            _normalsRTIdentifier = new RenderTargetIdentifier(_normalsRTID);
            _metaballRTIdentifier = new RenderTargetIdentifier(_metaballRTID);
        }

        // Execute is called for every eligible camera every frame. It's not called at the moment that
        // rendering is actually taking place, so don't directly execute rendering commands here.
        // Instead use the methods on ScriptableRenderContext to set up instructions.
        // RenderingData provides a bunch of (not very well documented) information about the scene
        // and what's being rendered.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = (_renderQueueType == RenderQueueType.Transparent)
                ? SortingCriteria.CommonTransparent
                : renderingData.cameraData.defaultOpaqueSortFlags;

            DrawingSettings drawingSettings =
                CreateDrawingSettings(_shaderTagIds, ref renderingData, sortingCriteria);


            // fetch a command buffer to use
            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);
            cmd.Clear();
            using (new ProfilingScope(cmd, new ProfilingSampler("Clear and set vertexColor render target")))
            { 
                cmd.Blit(_cameraColorTargetIdent, _metaballRTIdentifier);
                cmd.SetRenderTarget(_vertexColorRTIdentifier, _vertexColorRTIdentifier);
                cmd.ClearRenderTarget(true, true, Color.clear);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            drawingSettings.overrideMaterial = _vertexColorMaterial;
            using (new ProfilingScope(cmd, new ProfilingSampler("VertexColorDrawing")))
            { 
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings,
                ref _renderStateBlock);
                context.Submit();
            }

            using (new ProfilingScope(cmd, new ProfilingSampler("FirstBlit")))
            { 
                cmd.Blit(_vertexColorRTIdentifier, _metaballRTIdentifier, _metaBallBlitMaterial, 0);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            using (new ProfilingScope(cmd, new ProfilingSampler("Clear and set normals render target")))
            { 
                cmd.SetRenderTarget(_normalsRTIdentifier, _normalsRTIdentifier);
                //cmd.ClearRenderTarget(true, true, Color.clear);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            drawingSettings.overrideMaterial = _normalsMaterial;
            using (new ProfilingScope(cmd, new ProfilingSampler("NormalsDrawing")))
            { 
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings,
                ref _renderStateBlock);
                context.Submit();
            }

            using (new ProfilingScope(cmd, new ProfilingSampler("SecondBlit")))
            {
               cmd.Blit(_metaballRTIdentifier, _cameraColorTargetIdent);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        // called after Execute, use it to clean up anything allocated in Configure
        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_vertexColorRTID);
            cmd.ReleaseTemporaryRT(_metaballRTID);
        }
    }


    [System.Serializable]
    public class MyFeatureSettings
    {
        // we're free to put whatever we want here, public fields will be exposed in the inspector
        public bool IsEnabled = true;
        public RenderPassEvent WhenToInsert = RenderPassEvent.AfterRendering;
        public RenderQueueType RenderQueueType = RenderQueueType.Transparent;
        public Material VertexColorMaterial;
        public Material NormalsMaterial;
        public Material MetaballBlitMaterial;
        public Material FinalBlitMaterial;
        public LayerMask LayerMask;
        public string[] ShaderTags;
    }

    // MUST be named "settings" (lowercase) to be shown in the Render Features inspector
    public MyFeatureSettings settings = new MyFeatureSettings();

    MetaballRenderPass myRenderPass;

    public override void Create()
    {
        myRenderPass = new MetaballRenderPass(
            "MetaballRender",
            settings.WhenToInsert,
            settings.RenderQueueType,
            settings.VertexColorMaterial,
            settings.NormalsMaterial,
            settings.MetaballBlitMaterial,
            settings.FinalBlitMaterial,
            settings.LayerMask,
            settings.ShaderTags
        );
    }

    // called every frame once per camera
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!settings.IsEnabled)
        {
            // we can do nothing this frame if we want
            return;
        }

        // Gather up and pass any extra information our pass will need.
        // In this case we're getting the camera's color buffer target
        var cameraColorTargetIdent = renderer.cameraColorTarget;
        myRenderPass.Setup(cameraColorTargetIdent);

        // Ask the renderer to add our pass.
        // Could queue up multiple passes and/or pick passes to use
        renderer.EnqueuePass(myRenderPass);
    }
}


/*

RendererListDesc rendererListDesc = new RendererListDesc(
                _shaderTagIds,
                renderingData.cullResults,
                renderingData.cameraData.camera
            );

//rendererListDesc.layerMask = _layerMask;
//rendererListDesc.sortingCriteria = sortingCriteria;

RendererList list = context.CreateRendererList(rendererListDesc);

cmd.DrawRendererList(list);
context.ExecuteCommandBuffer(cmd);
cmd.Clear();

*/