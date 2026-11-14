using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Overworked
{
    /// <summary>
    /// CRT-эффект терминала пролога (сканлайны, рыбий глаз, виньетка, мерцание) — тот же приём, что
    /// GC26.Spinv.CrtCamera в Tiny UFO: полноэкранный проход с материалом после всей отрисовки кадра
    /// этой камеры. Общий рендерер проекта не меняется. Свой класс, потому что сборка Tiny UFO
    /// (GC26.Spinv) для Overworked закрыта.
    /// Эффект видит только то, что рисует камера: холст терминала должен быть Screen Space – Camera
    /// (PrologueTerminal переключает его сам). Затемнение FadePanel в HUD — overlay, поверх CRT.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class TerminalCrt : MonoBehaviour
    {
        [Tooltip("Материал на шейдере CRT из Tiny UFO (Materials/TerminalCRT)")]
        public Material material;

        Camera cam;
        CrtPass pass;

        void OnEnable()
        {
            cam = GetComponent<Camera>();
            if (pass == null) pass = new CrtPass();
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        }

        void OnBeginCamera(ScriptableRenderContext context, Camera rendering)
        {
            if (rendering != cam || material == null) return;
            var data = rendering.GetUniversalAdditionalCameraData();
            if (data == null || data.scriptableRenderer == null) return;
            pass.material = material;
            data.scriptableRenderer.EnqueuePass(pass);
        }

        class CrtPass : ScriptableRenderPass
        {
            public Material material;

            public CrtPass()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer) return;

                TextureHandle source = resources.activeColorTexture;
                TextureDesc desc = renderGraph.GetTextureDesc(source);
                desc.name = "_TerminalCrtColor";
                desc.clearBuffer = false;
                TextureHandle target = renderGraph.CreateTexture(desc);

                renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, target, material, 0), "Terminal CRT");
                resources.cameraColor = target;
            }
        }
    }
}
