# Copyright (c) 2024, NVIDIA CORPORATION.  All rights reserved.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

import os
import sys
sys.path.insert(0, os.getcwd())

from importlib import import_module
from code.common.constants import Benchmark, Scenario
from code.common.systems.system_list import KnownSystem
from configs.configuration import *

ParentConfig = import_module("configs.3d-unet")
GPUBaseConfig = ParentConfig.GPUBaseConfig


class OfflineGPUBaseConfig(GPUBaseConfig):
    scenario = Scenario.Offline
    gpu_inference_streams = 1
    gpu_copy_streams = 1
    gpu_batch_size = {'3d-unet': 4}
    slice_overlap_patch_kernel_cg_impl = False


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class GH200_96GB_aarch64x1(OfflineGPUBaseConfig):
    system = KnownSystem.GH200_96GB_ARMx1
    gpu_batch_size = {'3d-unet': 8}
    slice_overlap_patch_kernel_cg_impl = True
    offline_expected_qps = 6.8
    start_from_device = True
    end_on_device = True


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class GH200_96GB_aarch64x1_HighAccuracy(GH200_96GB_aarch64x1):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class L4x1(OfflineGPUBaseConfig):
    system = KnownSystem.L4x1
    gpu_batch_size = {'3d-unet': 1}
    offline_expected_qps = 1.3
    slice_overlap_patch_kernel_cg_impl = True


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class L4x1_HighAccuracy(L4x1):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class L40x1(OfflineGPUBaseConfig):
    system = KnownSystem.L40x1
    gpu_batch_size = {'3d-unet': 1}
    offline_expected_qps = 3
    slice_overlap_patch_kernel_cg_impl = True


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class L40Sx1(OfflineGPUBaseConfig):
    system = KnownSystem.L40Sx1
    gpu_batch_size = {'3d-unet': 1}
    offline_expected_qps = 3.8
    slice_overlap_patch_kernel_cg_impl = True


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class L40x1_HighAccuracy(L40x1):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class H200_SXM_141GBx1(OfflineGPUBaseConfig):
    system = KnownSystem.H200_SXM_141GBx1
    workspace_size = 128000000000
    gpu_batch_size = {'3d-unet': 8}
    offline_expected_qps = 6.8


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class H200_SXM_141GBx1_HighAccuracy(H200_SXM_141GBx1):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class H200_SXM_141GBx8(H200_SXM_141GBx1):
    system = KnownSystem.H200_SXM_141GBx8
    offline_expected_qps = 6.8 * 8

@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class H200_SXM_141GBx8_HighAccuracy(H200_SXM_141GBx8):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxQ)
class H200_SXM_141GBx8_MaxQ(H200_SXM_141GBx8):
    offline_expected_qps = 43
    power_limit = 350


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxQ)
class H200_SXM_141GBx8_HighAccuracy_MaxQ(H200_SXM_141GBx8):
    offline_expected_qps = 43
    power_limit = 350


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class H200_SXM_141GB_CTSx1(OfflineGPUBaseConfig):
    system = KnownSystem.H200_SXM_141GB_CTSx1
    workspace_size = 128000000000
    gpu_batch_size = {'3d-unet': 8}
    offline_expected_qps = 6.8


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class H200_SXM_141GB_CTSx8(H200_SXM_141GB_CTSx1):
    system = KnownSystem.H200_SXM_141GB_CTSx8
    offline_expected_qps = 6.8 * 8


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class H100_SXM_80GBx1(OfflineGPUBaseConfig):
    system = KnownSystem.H100_SXM_80GBx1
    # gpu_batch_size = {'3d-unet': 1}
    gpu_batch_size = {'3d-unet': 8}
    offline_expected_qps = 6.8


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class H100_SXM_80GBx1_Triton(H100_SXM_80GBx1):
    use_triton = True


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class H100_SXM_80GBx1_HighAccuracy(H100_SXM_80GBx1):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class H100_SXM_80GBx1_HighAccuracy_Triton(H100_SXM_80GBx1_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class H100_SXM_80GBx8(H100_SXM_80GBx1):
    system = KnownSystem.H100_SXM_80GBx8
    offline_expected_qps = 6.8 * 8


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxQ)
class H100_SXM_80GBx8_MaxQ(H100_SXM_80GBx8):
    offline_expected_qps = 38
    power_limit = 300


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class H100_SXM_80GBx8_Triton(H100_SXM_80GBx8):
    use_triton = True


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class H100_SXM_80GBx8_HighAccuracy(H100_SXM_80GBx8):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxQ)
class H100_SXM_80GBx8_HighAccuracy_MaxQ(H100_SXM_80GBx8_HighAccuracy):
    offline_expected_qps = 38
    power_limit = 300


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class H100_SXM_80GBx8_HighAccuracy_Triton(H100_SXM_80GBx8_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class H100_PCIe_80GBx1(OfflineGPUBaseConfig):
    system = KnownSystem.H100_PCIe_80GBx1
    # gpu_batch_size = {'3d-unet': 1}
    gpu_batch_size = {'3d-unet': 8}
    slice_overlap_patch_kernel_cg_impl = True
    offline_expected_qps = 4.8


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class H100_PCIe_80GBx1_Triton(H100_PCIe_80GBx1):
    use_triton = True


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class H100_PCIe_80GBx1_HighAccuracy(H100_PCIe_80GBx1):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class H100_PCIe_80GBx1_HighAccuracy_Triton(H100_PCIe_80GBx1_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class H100_PCIe_80GBx8(H100_PCIe_80GBx1):
    system = KnownSystem.H100_PCIe_80GBx8
    gpu_batch_size = {'3d-unet': 8}
    offline_expected_qps = 35


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxQ)
class H100_PCIe_80GBx8_MaxQ(H100_PCIe_80GBx8):
    offline_expected_qps = 26
    power_limit = 200


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class H100_PCIe_80GBx8_Triton(H100_PCIe_80GBx8):
    use_triton = True


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class H100_PCIe_80GBx8_HighAccuracy(H100_PCIe_80GBx8):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxQ)
class H100_PCIe_80GBx8_HighAccuracy_MaxQ(H100_PCIe_80GBx8_MaxQ):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class H100_PCIe_80GBx8_HighAccuracy_Triton(H100_PCIe_80GBx8_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class H100_PCIe_80GB_aarch64x1(OfflineGPUBaseConfig):
    system = KnownSystem.H100_PCIe_80GB_ARMx1
    gpu_batch_size = {'3d-unet': 1}
    offline_expected_qps = 4.9


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class H100_PCIe_80GB_aarch64x1_HighAccuracy(H100_PCIe_80GB_aarch64x1):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class H100_PCIe_80GB_aarch64x4(H100_PCIe_80GB_aarch64x1):
    system = KnownSystem.H100_PCIe_80GB_ARMx4
    gpu_batch_size = {'3d-unet': 1}
    offline_expected_qps = 17


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class H100_PCIe_80GB_aarch64x4_HighAccuracy(H100_PCIe_80GB_aarch64x4):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_80GBx1(OfflineGPUBaseConfig):
    system = KnownSystem.A100_PCIe_80GBx1
    gpu_batch_size = {'3d-unet': 8}
    offline_expected_qps = 3.5


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_80GBx1_HighAccuracy(A100_PCIe_80GBx1):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_80GBx1_Triton(A100_PCIe_80GBx1):
    use_triton = True


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_80GBx1_HighAccuracy_Triton(A100_PCIe_80GBx1_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_80GBx8(A100_PCIe_80GBx1):
    system = KnownSystem.A100_PCIe_80GBx8
    gpu_batch_size = {'3d-unet': 8}
    offline_expected_qps = 3.5 * 8
    numa_config = "3:0-15,128-143&2:16-31,144-159&1:32-47,160-175&0:48-63,176-191&7:64-79,192-207&6:80-95,208-223&5:96-111,224-239&4:112-127,240-255"


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_80GBx8_HighAccuracy(A100_PCIe_80GBx8):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_80GBx8_Triton(A100_PCIe_80GBx8):
    use_triton = True
    output_pinned_memory = False


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_80GBx8_HighAccuracy_Triton(A100_PCIe_80GBx8_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxQ)
class A100_PCIe_80GBx8_MaxQ(A100_PCIe_80GBx8):
    offline_expected_qps = 18.5
    power_limit = 165
    numa_config = "3:0-7&2:8-15&1:16-23&0:24-31&7:32-39&6:40-47&5:48-55&4:56-63"


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxQ)
class A100_PCIe_80GBx8_HighAccuracy_MaxQ(A100_PCIe_80GBx8_MaxQ):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxQ)
class A100_PCIe_80GBx8_Triton_MaxQ(A100_PCIe_80GBx8_MaxQ):
    use_triton = True
    offline_expected_qps = 17


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxQ)
class A100_PCIe_80GBx8_HighAccuracy_Triton_MaxQ(A100_PCIe_80GBx8_Triton_MaxQ):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_80GB_aarch64x1(OfflineGPUBaseConfig):
    system = KnownSystem.A100_PCIe_80GB_ARMx1
    gpu_batch_size = {'3d-unet': 8}
    offline_expected_qps = 2.5


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_80GB_aarch64x1_Triton(A100_PCIe_80GB_aarch64x1):
    use_triton = True
    output_pinned_memory = False


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_80GB_aarch64x1_HighAccuracy(A100_PCIe_80GB_aarch64x1):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_80GB_aarch64x1_HighAccuracy_Triton(A100_PCIe_80GB_aarch64x1_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_80GB_aarch64x2(OfflineGPUBaseConfig):
    system = KnownSystem.A100_PCIe_80GB_ARMx2
    gpu_batch_size = {'3d-unet': 8}
    offline_expected_qps = 5


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_80GB_aarch64x2_Triton(A100_PCIe_80GB_aarch64x2):
    output_pinned_memory = False
    use_triton = True


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_80GB_aarch64x2_HighAccuracy(A100_PCIe_80GB_aarch64x2):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_80GB_aarch64x2_HighAccuracy_Triton(A100_PCIe_80GB_aarch64x2_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_80GB_aarch64x4(OfflineGPUBaseConfig):
    system = KnownSystem.A100_PCIe_80GB_ARMx4
    gpu_batch_size = {'3d-unet': 8}
    offline_expected_qps = 10


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_80GB_aarch64x4_Triton(A100_PCIe_80GB_aarch64x4):
    output_pinned_memory = False
    gpu_copy_streams = True
    use_triton = True


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_80GB_aarch64x4_HighAccuracy(A100_PCIe_80GB_aarch64x4):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_80GB_aarch64x4_HighAccuracy_Triton(A100_PCIe_80GB_aarch64x4_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxQ)
class A100_PCIe_80GB_aarch64x4_MaxQ(OfflineGPUBaseConfig):
    system = KnownSystem.A100_PCIe_80GB_ARMx4
    gpu_batch_size = {'3d-unet': 8}
    power_limit = 175
    offline_expected_qps = 7.5


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxQ)
class A100_PCIe_80GB_aarch64x4_HighAccuracy_MaxQ(A100_PCIe_80GB_aarch64x4_MaxQ):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_aarch64x1(OfflineGPUBaseConfig):
    system = KnownSystem.A100_PCIe_40GB_ARMx1
    gpu_batch_size = {'3d-unet': 8}
    offline_expected_qps = 2.5


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_aarch64x1_Triton(A100_PCIe_aarch64x1):
    output_pinned_memory = False
    use_triton = True


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_aarch64x1_HighAccuracy(A100_PCIe_aarch64x1):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_aarch64x1_HighAccuracy_Triton(A100_PCIe_aarch64x1_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_aarch64x2(OfflineGPUBaseConfig):
    system = KnownSystem.A100_PCIe_40GB_ARMx2
    gpu_batch_size = {'3d-unet': 8}
    offline_expected_qps = 5


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_aarch64x2_Triton(A100_PCIe_aarch64x2):
    output_pinned_memory = False
    use_triton = True


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_aarch64x2_HighAccuracy(A100_PCIe_aarch64x2):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_aarch64x2_HighAccuracy_Triton(A100_PCIe_aarch64x2_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_aarch64x4(OfflineGPUBaseConfig):
    system = KnownSystem.A100_PCIe_40GB_ARMx4
    gpu_batch_size = {'3d-unet': 8}
    offline_expected_qps = 10


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_aarch64x4_Triton(A100_PCIe_aarch64x4):
    output_pinned_memory = False
    use_triton = True


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_aarch64x4_HighAccuracy(A100_PCIe_aarch64x4):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_aarch64x4_HighAccuracy_Triton(A100_PCIe_aarch64x4_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxQ)
class A100_PCIe_aarch64x4_MaxQ(OfflineGPUBaseConfig):
    system = KnownSystem.A100_PCIe_40GB_ARMx4
    gpu_batch_size = {'3d-unet': 8}
    power_limit = 175
    offline_expected_qps = 7.5


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxQ)
class A100_PCIe_aarch64x4_HighAccuracy_MaxQ(A100_PCIe_aarch64x4_MaxQ):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_MIG_1x1g5gb(OfflineGPUBaseConfig):
    system = KnownSystem.A100_PCIe_40GB_MIG_1x1g_5gb
    gpu_batch_size = {'3d-unet': 1}
    workspace_size = 1073741824
    offline_expected_qps = 0.35


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_MIG_1x1g5gb_HighAccuracy(A100_PCIe_MIG_1x1g5gb):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_MIG_1x1g5gb_Triton(A100_PCIe_MIG_1x1g5gb):
    use_triton = True


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_MIG_1x1g5gb_HighAccuracy_Triton(A100_PCIe_MIG_1x1g5gb_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_80GB_MIG_1x1g10gb(OfflineGPUBaseConfig):
    system = KnownSystem.A100_PCIe_80GB_MIG_1x1g_10gb
    workspace_size = 1073741824
    gpu_batch_size = {'3d-unet': 1}
    offline_expected_qps = 0.47


@ConfigRegistry.register(HarnessType.HeteroMIG, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_80GB_MIG_1x1g10gb_Hetero(A100_PCIe_80GB_MIG_1x1g10gb):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_80GB_MIG_1x1g10gb_HighAccuracy(A100_PCIe_80GB_MIG_1x1g10gb):
    pass


@ConfigRegistry.register(HarnessType.HeteroMIG, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_80GB_MIG_1x1g10gb_Hetero_HighAccuracy(A100_PCIe_80GB_MIG_1x1g10gb_HighAccuracy):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_PCIe_80GB_MIG_1x1g10gb_Triton(A100_PCIe_80GB_MIG_1x1g10gb):
    use_triton = True


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_PCIe_80GB_MIG_1x1g10gb_HighAccuracy_Triton(A100_PCIe_80GB_MIG_1x1g10gb_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM_80GB_MIG_1x1g10gb(OfflineGPUBaseConfig):
    system = KnownSystem.A100_SXM_80GB_MIG_1x1g_10gb
    workspace_size = 1073741824
    gpu_batch_size = {'3d-unet': 1}
    offline_expected_qps = 0.43
    start_from_device = True


@ConfigRegistry.register(HarnessType.HeteroMIG, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM_80GB_MIG_1x1g10gb_Hetero(A100_SXM_80GB_MIG_1x1g10gb):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM_80GB_MIG_1x1g10gb_HighAccuracy(A100_SXM_80GB_MIG_1x1g10gb):
    pass


@ConfigRegistry.register(HarnessType.HeteroMIG, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM_80GB_MIG_1x1g10gb_Hetero_HighAccuracy(A100_SXM_80GB_MIG_1x1g10gb_HighAccuracy):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM_80GB_MIG_1x1g10gb_Triton(A100_SXM_80GB_MIG_1x1g10gb):
    use_triton = True


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM_80GB_MIG_1x1g10gb_HighAccuracy_Triton(A100_SXM_80GB_MIG_1x1g10gb_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM_80GBx1(OfflineGPUBaseConfig):
    system = KnownSystem.A100_SXM_80GBx1
    offline_expected_qps = 3.8
    gpu_batch_size = {'3d-unet': 8}
    start_from_device = True
    end_on_device = True


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM_80GBx1_HighAccuracy(A100_SXM_80GBx1):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM_80GBx1_Triton(A100_SXM_80GBx1):
    instance_group_count = 1
    use_triton = True


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM_80GBx1_HighAccuracy_Triton(A100_SXM_80GBx1_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM_80GBx8(A100_SXM_80GBx1):
    system = KnownSystem.A100_SXM_80GBx8
    offline_expected_qps = 30
    use_cuda_thread_per_device = True


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM_80GBx8_HighAccuracy(A100_SXM_80GBx8):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM_80GBx8_Triton(A100_SXM_80GBx8):
    use_graphs = True
    use_triton = True
    output_pinned_memory = False


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM_80GBx8_HighAccuracy_Triton(A100_SXM_80GBx8_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxQ)
class A100_SXM_80GBx8_MaxQ(A100_SXM_80GBx8):
    gpu_batch_size = {'3d-unet': 8}
    offline_expected_qps = 21
    power_limit = 225


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxQ)
class A100_SXM_80GBx8_HighAccuracy_MaxQ(A100_SXM_80GBx8_MaxQ):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxQ)
class A100_SXM_80GBx8_Triton_MaxQ(A100_SXM_80GBx8_MaxQ):
    use_triton = True


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxQ)
class A100_SXM_80GBx8_HighAccuracy_Triton_MaxQ(A100_SXM_80GBx8_Triton_MaxQ):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM_80GB_aarch64x1(OfflineGPUBaseConfig):
    system = KnownSystem.A100_SXM_80GB_ARMx1
    gpu_batch_size = {'3d-unet': 8}
    offline_expected_qps = 3


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM_80GB_aarch64x1_HighAccuracy(A100_SXM_80GB_aarch64x1):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM_80GB_aarch64x1_Triton(A100_SXM_80GB_aarch64x1):
    use_triton = True


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM_80GB_aarch64x1_HighAccuracy_Triton(A100_SXM_80GB_aarch64x1_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM_80GB_aarch64x8(A100_SXM_80GB_aarch64x1):
    system = KnownSystem.A100_SXM_80GB_ARMx8
    offline_expected_qps = 24


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM_80GB_aarch64x8_HighAccuracy(A100_SXM_80GB_aarch64x8):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxQ)
class A100_SXM_80GB_aarch64x8_MaxQ(A100_SXM_80GB_aarch64x8):
    offline_expected_qps = 16
    power_limit = 200


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxQ)
class A100_SXM_80GB_aarch64x8_HighAccuracy_MaxQ(A100_SXM_80GB_aarch64x8_HighAccuracy):
    offline_expected_qps = 16
    power_limit = 200


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM_80GB_aarch64x8_Triton(A100_SXM_80GB_aarch64x8):
    use_graphs = True
    use_triton = True
    output_pinned_memory = False


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM_80GB_aarch64x8_HighAccuracy_Triton(A100_SXM_80GB_aarch64x8_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM_80GB_aarch64_MIG_1x1g10gb(OfflineGPUBaseConfig):
    system = KnownSystem.A100_SXM_80GB_ARM_MIG_1x1g_10gb
    workspace_size = 1073741824
    gpu_batch_size = {'3d-unet': 1}
    offline_expected_qps = 0.43


@ConfigRegistry.register(HarnessType.HeteroMIG, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM_80GB_aarch64_MIG_1x1g10gb_Hetero(A100_SXM_80GB_aarch64_MIG_1x1g10gb):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM_80GB_aarch64_MIG_1x1g10gb_HighAccuracy(A100_SXM_80GB_aarch64_MIG_1x1g10gb):
    pass


@ConfigRegistry.register(HarnessType.HeteroMIG, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM_80GB_aarch64_MIG_1x1g10gb_Hetero_HighAccuracy(A100_SXM_80GB_aarch64_MIG_1x1g10gb_HighAccuracy):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM_80GB_aarch64_MIG_1x1g10gb_Triton(A100_SXM_80GB_aarch64_MIG_1x1g10gb):
    use_triton = True


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM_80GB_aarch64_MIG_1x1g10gb_HighAccuracy_Triton(A100_SXM_80GB_aarch64_MIG_1x1g10gb_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM4_40GB_MIG_1x1g5gb(OfflineGPUBaseConfig):
    system = KnownSystem.A100_SXM4_40GB_MIG_1x1g_5gb
    workspace_size = 1073741824
    gpu_batch_size = {'3d-unet': 1}
    offline_expected_qps = 0.33
    start_from_device = True


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM4_40GB_MIG_1x1g5gb_HighAccuracy(A100_SXM4_40GB_MIG_1x1g5gb):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM4_40GB_MIG_1x1g5gb_Triton(A100_SXM4_40GB_MIG_1x1g5gb):
    use_triton = True


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM4_40GB_MIG_1x1g5gb_HighAccuracy_Triton(A100_SXM4_40GB_MIG_1x1g5gb_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM4_40GBx1(OfflineGPUBaseConfig):
    system = KnownSystem.A100_SXM4_40GBx1
    gpu_batch_size = {'3d-unet': 1}
    offline_expected_qps = 2.9
    start_from_device = True


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM4_40GBx1_HighAccuracy(A100_SXM4_40GBx1):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM4_40GBx1_Triton(A100_SXM4_40GBx1):
    use_triton = True


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM4_40GBx1_HighAccuracy_Triton(A100_SXM4_40GBx1_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM4_40GBx8(A100_SXM4_40GBx1):
    system = KnownSystem.A100_SXM4_40GBx8
    offline_expected_qps = 23


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM4_40GBx8_HighAccuracy(A100_SXM4_40GBx8):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM4_40GBx8_Triton(A100_SXM4_40GBx8):
    use_triton = True


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class A100_SXM4_40GBx8_HighAccuracy_Triton(A100_SXM4_40GBx8_Triton):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxQ)
class A100_SXM4_40GBx8_MaxQ(A100_SXM4_40GBx8):
    power_limit = 225


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxQ)
class A100_SXM4_40GBx8_HighAccuracy_MaxQ(A100_SXM4_40GBx8_MaxQ):
    pass


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99, PowerSetting.MaxQ)
class A100_SXM4_40GBx8_Triton_MaxQ(A100_SXM4_40GBx8_MaxQ):
    use_triton = True


@ConfigRegistry.register(HarnessType.Triton, AccuracyTarget.k_99_9, PowerSetting.MaxQ)
class A100_SXM4_40GBx8_HighAccuracy_Triton_MaxQ(A100_SXM4_40GBx8_Triton_MaxQ):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class Orin(OfflineGPUBaseConfig):
    system = KnownSystem.Orin
    gpu_batch_size = {'3d-unet': 2}
    offline_expected_qps = 0.45
    use_direct_host_access = True


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxQ)
class Orin_MaxQ(Orin):
    soc_cpu_freq = 576000
    soc_gpu_freq = 726750000
    soc_dla_freq = 0
    soc_emc_freq = 2133000000
    soc_pva_freq = 115000000
    orin_num_cores = 2
    offline_expected_qps = 0.31


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class Orin_HighAccuracy(Orin):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxQ)
class Orin_HighAccuracy_MaxQ(Orin_MaxQ):
    pass


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class Orin_NX(OfflineGPUBaseConfig):
    system = KnownSystem.Orin_NX
    gpu_batch_size = {'3d-unet': 2}
    offline_expected_qps = 0.17
    use_direct_host_access = True


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxQ)
class Orin_NX_MaxQ(Orin_NX):
    soc_cpu_freq = 576000
    soc_gpu_freq = 726750000
    soc_dla_freq = 0
    soc_emc_freq = 2133000000
    soc_pva_freq = 115000000
    orin_num_cores = 2
    offline_expected_qps = 0.17


@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99_9, PowerSetting.MaxP)
class Orin_NX_HighAccuracy(Orin_NX):
    pass