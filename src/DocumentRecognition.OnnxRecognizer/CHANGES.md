# Modifications from original RapidOCRCSharp

## General
- Removed System.Drawing dependencies for Linux compatibility
- Changed EmguCv to OpenCvSharp dependency
- Added entry point with Mat parameter
- Translated Chinese comments to English
- Renamed classes and methods according to naming rules and conventions
- Removed padding from returned textbox positions

## Project structure
- Models moved to separated files (`Ocr/Models`)
- OCR pipeline moved to `Ocr/` (`OcrLite`, `OcrUtils`, `TextDetector`, `TextLineOrientationClassifier`, `TextRecognizer`)
- Inference engines and contracts moved to `Inference/` (`OnnxSessionFactory`, `IInferenceEngine`, layout/orientation/table detectors)
- `InferenceExecutionProvider` moved to `Enums/`

## Models
- Added document orientation classification (`DocOrientationClassifier`)
- Added drawing layout detection (`DrawingLayoutDetector`)
- Added wired table cell detection (`WiredTableCellDetector`)
- Added GOST Cyrillic text recognition model and character dictionary support
- Added debug visualization helpers (`DrawDetections`) for layout and table-cell detectors

## Inference runtime
- Added `OnnxSessionFactory` as a single entry point for creating `InferenceSession` instances:
  graph optimization level, execution provider and thread count are configured in one place
- Added `InferenceExecutionProvider` (`Auto`, `Cpu`, `DirectMl`):
  - `Cpu` — CPU only;
  - `DirectMl` — DirectML only, no silent fallback;
  - `Auto` — DirectML with an extra probe inference, falling back to CPU on failure.
    The probe is required because DirectML can fail during `Run`, not only while creating a session,
    and ONNX Runtime does not switch execution providers automatically in that case
- All engines, including the OCR detector/classifier/recognizer, now create sessions through the factory;
  previously each engine duplicated its own DirectML/CPU fallback logic
- CPU thread count (`IntraOpNumThreads`/`InterOpNumThreads`) is applied for any provider;
  previously it was set only inside the DirectML failure path
- Added `IInferenceEngine` contract (`IsInitialized`, `InitializeAsync`) for lazy, idempotent
  and thread-safe model loading shared by all engines
- Engines are cheap to construct and expose model path, provider and thread count, so the host can
  reuse unchanged engines and recreate only the changed ones at runtime (hot reload of models
  without restarting the application)
- `OcrLite` propagates the selected provider and thread count to its three internal models
- Added provider initialization logging (`CPU initialized`, `DirectML initialized`, probe failures)
