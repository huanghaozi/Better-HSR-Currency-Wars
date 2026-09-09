# OCRRuntime

本目录存放 OCR 模型文件，供 `RapidOcrNet`（纯 C# 实现）加载。

```text
OCRRuntime\models\v6\PP-OCRv6_det_small.onnx
OCRRuntime\models\v6\PP-OCRv6_rec_small.onnx
OCRRuntime\models\v6\ppocrv6_small_dict.txt
```

模型来自 PaddleOCR 的 PP-OCRv6 small 档，由 RapidOCR 项目转换为 ONNX 格式，许可为 Apache-2.0。

下载地址：

- https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.9.2/onnx/PP-OCRv6/det/PP-OCRv6_det_small.onnx
- https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.9.2/onnx/PP-OCRv6/rec/PP-OCRv6_rec_small.onnx
- https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.9.2/paddle/PP-OCRv6/rec/PP-OCRv6_rec_small/ppocrv6_dict.txt

字典文件需重命名为 `ppocrv6_small_dict.txt`，以匹配 RapidOcrNet 的 `PPOCRv6Small` 预设。

旧的 Python 桥接方案（`rapidocr_bridge.exe` / `rapidocr_bridge.py`）已移除，不再随包分发。

