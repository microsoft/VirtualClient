# Metaseq

Metaseq is codebase for working with [Open Pre-trained Transformers](https://github.com/facebookresearch/metaseq/tree/main/projects/OPT), originally forked from [fairseq](https://github.com/facebookresearch/fairseq).

* [Metaseq Documentation](https://github.com/facebookresearch/metaseq/tree/main/docs)  
* [Metaseq Github Repo](https://github.com/facebookresearch/metaseq)  
* [Open Pre-trained Transformers](https://github.com/facebookresearch/metaseq/tree/main/projects/OPT)
* [Fairseq](https://github.com/facebookresearch/fairseq)
* [Metaseq Setup](https://github.com/facebookresearch/metaseq/blob/main/docs/setup.md)

## System Requirements

This is a GPU-specific workload and requires high-performance graphic cards to run. It is recommended that the system-under-test have a high-performing Nvidia (e.g. M60 or higher) or AMD (e.g. MI25 or higher)
graphics card.

## Supported Hardware Systems

Currently, Metaseq is only supported for Nvidia GPUs through Virtual Client.
Work in Progress for AMD GPUs.

## Workload Metrics Metaseq

The following metrics are examples of those captured by the Virtual Client when running the Metaseq workload.

|Scenario | Metric Name  | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|---------|--------------|---------------------|---------------------|---------------------|------|
| bert | DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_9_MaxP-Offline-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_9_MaxP-Offline-PerformanceMode | 1.0 | 1.0 | 1.0 | VALID/INVALID |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_9_MaxP-Server-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_9_MaxP-Server-PerformanceMode | 0.0 | 1.0 | 0.5333333333333333 | VALID/INVALID |
