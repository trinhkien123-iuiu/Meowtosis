#if UNITY_EDITOR
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace Meowtosis.Diagnostics
{
    // Công cụ đo hiệu năng cho editor (không vào bản build): gắn vào một đối tượng trong Play mode, đo `seconds` giây rồi
    // ghi kết quả vào PerfProbe.Report: thời gian khung trung bình và xấu nhất, cấp phát GC mỗi khung, số draw call,
    // batch, SetPass. Dùng để so sánh trước/sau khi tối ưu (số trong editor chậm hơn máy thật, chỉ có giá trị so sánh).
    public sealed class PerfProbe : MonoBehaviour
    {
        public static string Report = "";
        public float seconds = 4f;

        private ProfilerRecorder gcAllocated;
        private ProfilerRecorder drawCalls;
        private ProfilerRecorder batches;
        private ProfilerRecorder setPass;
        private ProfilerRecorder triangles;
        private ProfilerRecorder buildBatch;
        private ProfilerRecorder updateBatches;
        private ProfilerRecorder textGenerate;
        private double buildBatchSum;
        private double updateBatchesSum;
        private double textGenerateSum;
        private float elapsed;
        private int frames;
        private float worst;
        private double gcSum;
        private double drawSum;
        private double batchSum;
        private double setPassSum;
        private double triangleSum;
        private float frameSum;

        private void OnEnable()
        {
            gcAllocated = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            buildBatch = ProfilerRecorder.StartNew(new ProfilerCategory("UI Render"), "Canvas.BuildBatch");
            updateBatches = ProfilerRecorder.StartNew(new ProfilerCategory("UI Render"), "UGUI.Rendering.UpdateBatches");
            textGenerate = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "TMP.GenerateText");
            Report = "";
        }

        private void OnDisable()
        {
            gcAllocated.Dispose();
            drawCalls.Dispose();
            batches.Dispose();
            setPass.Dispose();
            triangles.Dispose();
            buildBatch.Dispose();
            updateBatches.Dispose();
            textGenerate.Dispose();
        }

        private void Update()
        {
            if (Report.Length > 0) return;
            elapsed += Time.unscaledDeltaTime;
            if (elapsed < 1f) return;   // bỏ giây đầu cho ổn định
            frames++;
            frameSum += Time.unscaledDeltaTime;
            worst = Mathf.Max(worst, Time.unscaledDeltaTime);
            gcSum += gcAllocated.LastValue;
            drawSum += drawCalls.LastValue;
            batchSum += batches.LastValue;
            setPassSum += setPass.LastValue;
            triangleSum += triangles.LastValue;
            buildBatchSum += buildBatch.LastValue;
            updateBatchesSum += updateBatches.LastValue;
            textGenerateSum += textGenerate.LastValue;
            if (elapsed < 1f + seconds) return;
            var text = new StringBuilder();
            text.Append($"khung {frames} | trung bình {frameSum / frames * 1000f:0.00} ms ({frames / frameSum:0} fps) | xấu nhất {worst * 1000f:0.0} ms");
            text.Append($" | GC {gcSum / frames / 1024.0:0.00} KB/khung | draw {drawSum / frames:0} | batch {batchSum / frames:0} | setpass {setPassSum / frames:0} | tam giác {triangleSum / frames:0}");
            text.Append($" | UI: BuildBatch {buildBatchSum / frames / 1000.0:0} us, UpdateBatches {updateBatchesSum / frames / 1000.0:0} us, TMP {textGenerateSum / frames / 1000.0:0} us/khung");
            Report = text.ToString();
        }
    }
}
#endif
