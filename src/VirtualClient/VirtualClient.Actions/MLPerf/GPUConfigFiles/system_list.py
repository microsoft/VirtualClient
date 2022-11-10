# Copyright (c) 2022, NVIDIA CORPORATION.  All rights reserved.
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

from __future__ import annotations
from enum import Enum
from typing import Any, Callable, Final, Optional, List, Union

import os
import importlib.util

from code.common.constants import *
from code.common.systems.base import *
from code.common.systems.accelerator import AcceleratorConfiguration, GPU, MIG
from code.common.systems.cpu import CPUConfiguration, CPU
from code.common.systems.memory import MemoryConfiguration
from code.common.systems.systems import SystemConfiguration
from code.common.systems.known_hardware import *


# Dynamically build Enum for known systems
system_confs = dict()


def add_systems(name_format_string: str, id_format_string: str, cpu: KnownCPU, accelerator: KnownGPU,
                accelerator_counts: List[int], mem_requirement: Memory):
    """Adds a SystemConfiguration to system_confs.

    Args:
        name_format_string (str): A Python format to generate the name for the Enum member. Can have a single format
                                  item to represent the count.
        id_format_string (str): A Python format to generate the system ID to use. The system ID is used for the systems/
                                json file. Can contain a single format item to represent the count.
        cpu (KnownCPU): The CPU that the system uses
        accelerator (KnownGPU): The Accelerator that the system uses
        accelerator_counts (List[int]): The list of various counts to use for accelerators.
        mem_requirement (Memory): The minimum memory requirement to have been tested for the hardware configuration.
    """
    for count in accelerator_counts:
        system_confs[name_format_string.format(count)] = SystemConfiguration(
            CPUConfiguration({cpu: MATCH_ANY}),
            min_memory_requirement(mem_requirement),
            AcceleratorConfiguration({accelerator: count}),
            numa_conf=MATCH_ANY,
            system_id=id_format_string.format(count))


# A100_PCIe_40GB and 80GB based systems:
add_systems("A100_PCIe_40GBx{}", "A100-PCIex{}",
            MatchAllowList([KnownCPU.AMD_EPYC_7742.value, KnownCPU.x86_64_Generic.value]),
            KnownGPU.A100_PCIe_40GB.value, [1, 8], Memory(30, ByteSuffix.GiB))
add_systems("A100_PCIe_40GB_ARMx{}", "A100-PCIe_aarch64x{}", KnownCPU.Neoverse_N1_ARM.value,
            KnownGPU.A100_PCIe_40GB.value, [1, 2, 4], Memory(30, ByteSuffix.GiB))
add_systems("A100_PCIe_80GBx{}", "A100-PCIe-80GBx{}",
            MatchAllowList([KnownCPU.AMD_EPYC_7742.value, KnownCPU.x86_64_Generic.value]),
            KnownGPU.A100_PCIe_80GB.value, [1, 2, 4, 8], Memory(30, ByteSuffix.GiB))
add_systems("A100_PCIe_80GB_ARMx{}", "A100-PCIe-80GB_aarch64x{}", KnownCPU.Neoverse_N1_ARM.value,
            KnownGPU.A100_PCIe_80GB.value, [1, 2, 4], Memory(30, ByteSuffix.GiB))

add_systems("A100_PCIe_40GB_MIG_{}x1g_5gb", "A100-PCIe-MIG_{}x1g.5gb", KnownCPU.AMD_EPYC_7742.value,
            KnownMIG.A100_PCIe_40GB_1GPC.value, [1], Memory(1, ByteSuffix.TiB))
add_systems("A100_PCIe_40GB_ARM_MIG_{}x1g_5gb", "A100-PCIe_aarch64-MIG_{}x1g.5gb", KnownCPU.Neoverse_N1_ARM.value,
            KnownMIG.A100_PCIe_40GB_1GPC.value, [1], Memory(1, ByteSuffix.TiB))
# FIXME: not sure what's causing ipp1-1468's host memory to be less than 1 TiB, so using 1TB for now
add_systems("A100_PCIe_80GB_MIG_{}x1g_10gb", "A100-PCIe-80GB-MIG_{}x1g.10gb", KnownCPU.AMD_EPYC_7742.value,
            KnownMIG.A100_PCIe_80GB_1GPC.value, [1, 7, 56], Memory(1, ByteSuffix.TB))
add_systems("A100_PCIe_80GB_ARM_MIG_{}x1g_10gb", "A100-PCIe-80GB_aarch64-MIG_{}x1g.10gb",
            KnownCPU.Neoverse_N1_ARM.value, KnownMIG.A100_PCIe_80GB_1GPC.value, [1], Memory(1, ByteSuffix.TiB))


# A100_SXM4_40GB and SXM_80GB based systems:
add_systems("A100_SXM4_40GBx{}", "DGX-A100_A100-SXM4-40GBx{}",
            MatchAllowList([KnownCPU.AMD_EPYC_7742.value, KnownCPU.x86_64_Generic.value]),
            KnownGPU.A100_SXM4_40GB.value, [1, 8], Memory(30, ByteSuffix.GiB))
add_systems("A100_SXM_80GBx{}", "DGX-A100_A100-SXM-80GBx{}",
            MatchAllowList([KnownCPU.AMD_EPYC_7742.value, KnownCPU.x86_64_Generic.value]),
            KnownGPU.A100_SXM_80GB.value, [1, 8], Memory(30, ByteSuffix.GiB))
add_systems("A100_SXM_80GB_ROx{}", "DGX-Station-A100_A100-SXM-80GBx{}",
            MatchAllowList([KnownCPU.AMD_EPYC_7742.value, KnownCPU.x86_64_Generic.value]),
            KnownGPU.A100_SXM_80GB_RO.value, [1, 4], Memory(30, ByteSuffix.GiB))
add_systems("A100_SXM_80GB_ARMx{}", "A100-SXM-80GB_aarch64x{}", KnownCPU.Neoverse_N1_ARM.value,
            KnownGPU.A100_SXM_80GB.value, [1, 8], Memory(1, ByteSuffix.TB))

add_systems("A100_SXM4_40GB_MIG_{}x1g_5gb", "DGX-A100_A100-SXM4-40GB-MIG_{}x1g.5gb", KnownCPU.AMD_EPYC_7742.value,
            KnownMIG.A100_SXM4_40GB_1GPC.value, [1, 56], Memory(1, ByteSuffix.TiB))
add_systems("A100_SXM_80GB_MIG_{}x1g_10gb", "DGX-A100_A100-SXM-80GB-MIG_{}x1g.10gb", KnownCPU.AMD_EPYC_7742.value,
            KnownMIG.A100_SXM_80GB_1GPC.value, [1, 7, 56], Memory(1, ByteSuffix.TiB))
add_systems("A100_SXM_80GB_RO_MIG_{}x1g_10gb", "DGX-Station-A100_A100-SXM-80GB-MIG_{}x1g.10gb",
            KnownCPU.AMD_EPYC_7742.value, KnownMIG.A100_SXM_80GB_RO_1GPC.value, [1, 7, 28],
            Memory(1, ByteSuffix.TiB))
add_systems("A100_SXM_80GB_ARM_MIG_{}x1g_10gb", "A100-SXM-80GB_aarch64_MIG_{}x1g.10gb",
            KnownCPU.Neoverse_N1_ARM.value, KnownMIG.A100_SXM_80GB_1GPC.value, [1, 7, 56],
            Memory(1, ByteSuffix.TB))

# Other Ampere based systems
add_systems("GeForceRTX_3080x{}", "GeForceRTX3080x{}", MATCH_ANY, KnownGPU.GeForceRTX_3080.value,
            [1], Memory(30, ByteSuffix.GiB))
add_systems("GeForceRTX_3090x{}", "GeForceRTX3090x{}", MATCH_ANY, KnownGPU.GeForceRTX_3090.value,
            [1], Memory(30, ByteSuffix.GiB))
add_systems("A10x{}", "A10x{}", KnownCPU.AMD_EPYC_7742.value, KnownGPU.A10.value,
            [1, 8], Memory(0.9, ByteSuffix.TiB))
add_systems("A30x{}", "A30x{}", KnownCPU.AMD_EPYC_7742.value, KnownGPU.A30.value,
            [1, 8], Memory(0.5, ByteSuffix.TiB))
add_systems("A2x{}", "A2x{}", MATCH_ANY, KnownGPU.A2.value, [1, 2], Memory(100, ByteSuffix.GiB))

add_systems("A30_MIG_{}x1g_6gb", "A30-MIG_{}x1g.6gb", KnownCPU.AMD_EPYC_7742.value, KnownMIG.A30_1GPC.value,
            [1, 32], Memory(0.5, ByteSuffix.TiB))

add_systems("DRIVE_A100_PCIE", "Drive-A100-PCIex1",
            KnownCPU.Intel_Xeon_Silver_4314.value, KnownGPU.DRIVE_A100_PCIE.value,
            [1], Memory(quantity=30.0, byte_suffix=ByteSuffix.GB))

# Turing based systems
add_systems("T4x{}", "T4x{}", CPU(MATCH_ANY, CPUArchitecture.x86_64, MATCH_ANY, MATCH_ANY), KnownGPU.T4.value,
            [1, 8, 20], Memory(32, ByteSuffix.GiB))

# Embedded systems
add_systems("AGX_Xavier", "AGX_Xavier", KnownCPU.NVIDIA_Carmel_ARM_V8.value, KnownGPU.AGX_Xavier.value,
            [1], Memory(30, ByteSuffix.GiB))
add_systems("Xavier_NX", "Xavier_NX", KnownCPU.NVIDIA_Carmel_ARM_V8.value, KnownGPU.Xavier_NX.value,
            [1], Memory(7, ByteSuffix.GiB))
add_systems("Orin", "Orin", KnownCPU.ARM_V8_Generic.value, KnownGPU.Orin.value,
            [1], Memory(7, ByteSuffix.GiB))

# CPU-based systems (no GPU/accelerators)
system_confs["Triton_CPU_2S_8380"] = SystemConfiguration(
    CPUConfiguration({KnownCPU.Intel_Xeon_Platinum_8380.value: 2}),
    min_memory_requirement(Memory(500, ByteSuffix.GB)),
    AcceleratorConfiguration(dict()),
    numa_conf=None,
    system_id="Triton_CPU_2S_8380x1")

system_confs["Triton_CPU_4S_8380H"] = SystemConfiguration(
    CPUConfiguration({KnownCPU.Intel_Xeon_Platinum_8380H.value: 4}),
    min_memory_requirement(Memory(500, ByteSuffix.GB)),
    AcceleratorConfiguration(dict()),
    numa_conf=None,
    system_id="Triton_CPU_4S_8380Hx1")

system_confs["Triton_CPU_2S_6258R"] = SystemConfiguration(
    CPUConfiguration({KnownCPU.Intel_Xeon_Gold_6258R.value: 2}),
    min_memory_requirement(Memory(500, ByteSuffix.GB)),
    AcceleratorConfiguration(dict()),
    numa_conf=None,
    system_id="Triton_CPU_2S_6258Rx1")


# Handle custom systems to better support partner drops
if importlib.util.find_spec("code.common.systems.custom_list") is not None:
    from code.common.systems.custom_list import custom_systems
    system_confs.update(custom_systems)

system_confs["Triton_Inferentia_INF1_XLARGE"] = SystemConfiguration(
    MATCH_ANY,
    MATCH_ANY,
    AcceleratorConfiguration({KnownInferentia.Inferentia_INF1_XLARGE.value: 1}),
    numa_conf=None,
    system_id="Triton_Inferentia_INF1_XLARGEx1"
)

system_confs["Triton_Inferentia_INF1_2XLARGE"] = SystemConfiguration(
    MATCH_ANY,
    MATCH_ANY,
    AcceleratorConfiguration({KnownInferentia.Inferentia_INF1_2XLARGE.value: 1}),
    numa_conf=None,
    system_id="Triton_Inferentia_INF1_2XLARGEx1"
)

system_confs["Triton_Inferentia_INF1_6XLARGE"] = SystemConfiguration(
    MATCH_ANY,
    MATCH_ANY,
    AcceleratorConfiguration({KnownInferentia.Inferentia_INF1_6XLARGE.value: 1}),
    numa_conf=None,
    system_id="Triton_Inferentia_INF1_6XLARGEx1"
)

system_confs["Triton_Inferentia_INF1_24XLARGE"] = SystemConfiguration(
    MATCH_ANY,
    MATCH_ANY,
    AcceleratorConfiguration({KnownInferentia.Inferentia_INF1_24XLARGE.value: 1}),
    numa_conf=None,
    system_id="Triton_Inferentia_INF1_24XLARGEx1"
)
KnownSystem = Enum("KnownSystem", system_confs)


def match_known_system(sys_conf):
    """Matches a SystemConfiguration with KnownSystems and returns the enum member sys_conf matched with. Also sets the
    system_id field of sys_conf to the system_id of the enum member.

    Returns None if no match was found."""
    for sys in KnownSystem:
        if sys_conf == sys.value:
            sys_conf.set_id(sys.value.system_id)
            return sys
    return None


DETECTED_SYSTEM = SystemConfiguration.detect()
"""SystemConfiguration: The detected SystemConfiguration of the current system at runtime"""

MATCHED_SYSTEM = match_known_system(DETECTED_SYSTEM)
"""KnownSystem: The KnownSystem Enum member that DETECTED_SYSTEM matched with. Used to select BenchmarkConfigurations."""


def _default_on_matched(f):
    """Decorator that takes in a classmethod Callable that takes in a System as a parameter, and returns an equivalent
    Callable where that single parameter now has a default value of MATCHED_SYSTEM.
    """
    def _inner(cls, v=MATCHED_SYSTEM):
        # Make sure that v is a KnownSystem
        if v is None:
            return False
        elif type(v) is SystemConfiguration:
            v = match_known_system(v)
        elif type(v) is not KnownSystem:
            raise TypeError(f"Cannot classify system of object type {type(v)}")
        return f(cls, v)
    return _inner


class SystemClassifications:
    """Defines classmethods with the signature Callable[KnownSystem, bool] that returns True or False, representing
    whether or not a KnownSystem satisfies a certain condition.

    This is the equivalent of code.common.constants.AdHocSystemClassification for the old System description from MLPerf
    Inference v1.1."""

    @classmethod
    @_default_on_matched
    def is_xavier_nx(cls, sys):
        return sys == KnownSystem.Xavier_NX

    @classmethod
    @_default_on_matched
    def is_xavier_agx(cls, sys):
        return sys == KnownSystem.AGX_Xavier

    @classmethod
    @_default_on_matched
    def is_xavier(cls, sys):
        return SystemClassifications.is_xavier_nx(sys) or SystemClassifications.is_xavier_agx(sys)

    @classmethod
    @_default_on_matched
    def is_ampere(cls, sys):
        return sys.value.get_compute_sm() in [80, 86, 87]

    @classmethod
    @_default_on_matched
    def is_turing(cls, sys):
        return sys.value.get_compute_sm() == 75

    @classmethod
    @_default_on_matched
    def is_orin(cls, sys):
        return sys in [KnownSystem.Orin]

    @classmethod
    @_default_on_matched
    def is_soc(cls, sys):
        return SystemClassifications.is_xavier(sys) or SystemClassifications.is_orin(sys)

    @classmethod
    @_default_on_matched
    def start_from_device_enabled(cls, sys):
        return sys in [
            KnownSystem.A100_SXM_80GBx1,
            KnownSystem.A100_SXM_80GBx8,
            KnownSystem.A100_SXM_80GB_MIG_1x1g_10gb,
            KnownSystem.A100_SXM_80GB_MIG_56x1g_10gb,
            KnownSystem.A100_SXM4_40GBx1,
            KnownSystem.A100_SXM4_40GBx8,
            KnownSystem.A100_SXM4_40GB_MIG_1x1g_5gb,
            KnownSystem.A100_SXM_80GB_ARMx1,
            KnownSystem.A100_SXM_80GB_ARMx8,
        ]

    @classmethod
    @_default_on_matched
    def end_on_device_enabled(cls, sys):
        return sys in [
            KnownSystem.A100_SXM_80GBx1,
            KnownSystem.A100_SXM_80GBx8,
            KnownSystem.A100_SXM_80GB_MIG_1x1g_10gb,
            KnownSystem.A100_SXM_80GB_MIG_56x1g_10gb,
            KnownSystem.A100_SXM4_40GBx1,
            KnownSystem.A100_SXM4_40GBx8,
            KnownSystem.A100_SXM4_40GB_MIG_1x1g_5gb,
            KnownSystem.A100_SXM_80GB_ARMx1,
            KnownSystem.A100_SXM_80GB_ARMx8,
        ]

    @classmethod
    @_default_on_matched
    def intel_openvino(cls, sys):
        return sys in [KnownSystem.Triton_CPU_2S_8380, KnownSystem.Triton_CPU_4S_8380H, KnownSystem.Triton_CPU_2S_6258R]

    @classmethod
    @_default_on_matched
    def inferentia_based(cls, sys):
        return sys in [KnownSystem.Triton_Inferentia_INF1_XLARGE, KnownSystem.Triton_Inferentia_INF1_2XLARGE, KnownSystem.Triton_Inferentia_INF1_6XLARGE, KnownSystem.Triton_Inferentia_INF1_24XLARGE]

    @classmethod
    @_default_on_matched
    def gpu_based(cls, sys):
        return sys in [
            known
            for known in KnownSystem
            if known.value.accelerator_conf.num_gpus() + known.value.accelerator_conf.num_migs() > 0
        ]

    @classmethod
    @_default_on_matched
    def multi_gpu(cls, sys):
        return sys in [
            known
            for known in KnownSystem
            if known.value.accelerator_conf.num_gpus() + known.value.accelerator_conf.num_migs() > 1
        ]
