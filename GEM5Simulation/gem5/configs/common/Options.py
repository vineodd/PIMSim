# Copyright (c) 2013 ARM Limited
# All rights reserved.
#
# The license below extends only to copyright in the software and shall
# not be construed as granting a license to any other intellectual
# property including but not limited to intellectual property relating
# to a hardware implementation of the functionality of the software
# licensed hereunder.  You may use the software subject to the license
# terms below provided that you ensure that this notice is replicated
# unmodified and in its entirety in all distributions of the software,
# modified or unmodified, in source code or in binary form.
#
# Copyright (c) 2006-2008 The Regents of The University of Michigan
# All rights reserved.
#
# Redistribution and use in source and binary forms, with or without
# modification, are permitted provided that the following conditions are
# met: redistributions of source code must retain the above copyright
# notice, this list of conditions and the following disclaimer;
# redistributions in binary form must reproduce the above copyright
# notice, this list of conditions and the following disclaimer in the
# documentation and/or other materials provided with the distribution;
# neither the name of the copyright holders nor the names of its
# contributors may be used to endorse or promote products derived from
# this software without specific prior written permission.
#
# THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
# "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
# LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
# A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
# OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
# SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
# LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
# DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
# THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
# (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
# OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#
# Authors: Lisa Hsu

import m5
from m5.defines import buildEnv
from m5.objects import *
from common.Benchmarks import *

from common import CpuConfig
from common import BPConfig
from common import MemConfig
from common import PlatformConfig

def _listCpuTypes(option, opt, value, parser):
    CpuConfig.print_cpu_list()
    sys.exit(0)

def _listBPTypes(option, opt, value, parser):
    BPConfig.print_bp_list()
    sys.exit(0)

def _listMemTypes(option, opt, value, parser):
    MemConfig.print_mem_list()
    sys.exit(0)

def _listPlatformTypes(option, opt, value, parser):
    PlatformConfig.print_platform_list()
    sys.exit(0)

# Add the very basic options that work also in the case of the no ISA
# being used, and consequently no CPUs, but rather various types of
# testers and traffic generators.
def addNoISAOptions(parser):
    parser.add_option("-n", "--num-cpus", type="int", default=1)
    parser.add_option("--sys-voltage", action="store", type="string",
                      default='1.0V',
                      help = """Top-level voltage for blocks running at system
                      power supply""")
    parser.add_option("--sys-clock", action="store", type="string",
                      default='1GHz',
                      help = """Top-level clock for blocks running at system
                      speed""")

    # Memory Options
    parser.add_option("--list-mem-types",
                      action="callback", callback=_listMemTypes,
                      help="List available memory types")
    parser.add_option("--mem-type", type="choice", default="DDR3_1600_8x8",
                      choices=MemConfig.mem_names(),
                      help = "type of memory to use")
    parser.add_option("--mem-channels", type="int", default=1,
                      help = "number of memory channels")
    parser.add_option("--mem-ranks", type="int", default=1,
                      help = "number of memory ranks per channel")
    parser.add_option("--mem-size", action="store", type="string",
                      default="512MB",
                      help="Specify the physical memory size (single memory)")


    parser.add_option("--memchecker", action="store_true")

    # Cache Options
    parser.add_option("--external-memory-system", type="string",
                      help="use external ports of this port_type for caches")
    parser.add_option("--tlm-memory", type="string",
                      help="use external port for SystemC TLM cosimulation")
    parser.add_option("--caches", action="store_true")
    parser.add_option("--l2cache", action="store_true")
    parser.add_option("--num-dirs", type="int", default=1)
    parser.add_option("--num-l2caches", type="int", default=1)
    parser.add_option("--num-l3caches", type="int", default=1)
    parser.add_option("--l1d_size", type="string", default="64kB")
    parser.add_option("--l1i_size", type="string", default="32kB")
    parser.add_option("--l2_size", type="string", default="2MB")
    parser.add_option("--l3_size", type="string", default="16MB")
    parser.add_option("--l1d_assoc", type="int", default=2)
    parser.add_option("--l1i_assoc", type="int", default=2)
    parser.add_option("--l2_assoc", type="int", default=8)
    parser.add_option("--l3_assoc", type="int", default=16)
    parser.add_option("--cacheline_size", type="int", default=64)

    # Enable Ruby
    parser.add_option("--ruby", action="store_true")

    # Run duration options
    parser.add_option("-m", "--abs-max-tick", type="int", default=m5.MaxTick,
                      metavar="TICKS", help="Run to absolute simulated tick "
                      "specified including ticks from a restored checkpoint")
    parser.add_option("--rel-max-tick", type="int", default=None,
                      metavar="TICKS", help="Simulate for specified number of"
                      " ticks relative to the simulation start tick (e.g. if "
                      "restoring a checkpoint)")
    parser.add_option("--maxtime", type="float", default=None,
                      help="Run to the specified absolute simulated time in "
                      "seconds")
    parser.add_option("-P", "--param", action="append", default=[],
        help="Set a SimObject parameter relative to the root node. "
             "An extended Python multi range slicing syntax can be used "
             "for arrays. For example: "
             "'system.cpu[0,1,3:8:2].max_insts_all_threads = 42' "
             "sets max_insts_all_threads for cpus 0, 1, 3, 5 and 7 "
             "Direct parameters of the root object are not accessible, "
             "only parameters of its children.")

# Add common options that assume a non-NULL ISA.
def addCommonOptions(parser):
    # start by adding the base options that do not assume an ISA
    addNoISAOptions(parser)

    # system options
    parser.add_option("--list-cpu-types",
                      action="callback", callback=_listCpuTypes,
                      help="List available CPU types")
    parser.add_option("--cpu-type", type="choice", default="AtomicSimpleCPU",
                      choices=CpuConfig.cpu_names(),
                      help = "type of cpu to run with")
    parser.add_option("--list-bp-types",
                      action="callback", callback=_listBPTypes,
                      help="List available branch predictor types")
    parser.add_option("--bp-type", type="choice", default=None,
                      choices=BPConfig.bp_names(),
                      help = """
                      type of branch predictor to run with
                      (if not set, use the default branch predictor of
                      the selected CPU)""")
    parser.add_option("--checker", action="store_true");
    parser.add_option("--cpu-clock", action="store", type="string",
                      default='2GHz',
                      help="Clock for blocks running at CPU speed")
    parser.add_option("--smt", action="store_true", default=False,
                      help = """
                      Only used if multiple programs are specified. If true,
                      then the number of threads per cpu is same as the
                      number of programs.""")
    parser.add_option("--elastic-trace-en", action="store_true",
                      help="""Enable capture of data dependency and instruction
                      fetch traces using elastic trace probe.""")
    # Trace file paths input to trace probe in a capture simulation and input
    # to Trace CPU in a replay simulation
    parser.add_option("--inst-trace-file", action="store", type="string",
                      help="""Instruction fetch trace file input to
                      Elastic Trace probe in a capture simulation and
                      Trace CPU in a replay simulation""", default="")
    parser.add_option("--data-trace-file", action="store", type="string",
                      help="""Data dependency trace file input to
                      Elastic Trace probe in a capture simulation and
                      Trace CPU in a replay simulation""", default="")

    parser.add_option("-l", "--lpae", action="store_true")
    parser.add_option("-V", "--virtualisation", action="store_true")

    # dist-gem5 options
    parser.add_option("--dist", action="store_true",
                      help="Parallel distributed gem5 simulation.")
    parser.add_option("--dist-sync-on-pseudo-op", action="store_true",
                      help="Use a pseudo-op to start dist-gem5 synchronization.")
    parser.add_option("--is-switch", action="store_true",
                      help="Select the network switch simulator process for a"\
                      "distributed gem5 run")
    parser.add_option("--dist-rank", default=0, action="store", type="int",
                      help="Rank of this system within the dist gem5 run.")
    parser.add_option("--dist-size", default=0, action="store", type="int",
                      help="Number of gem5 processes within the dist gem5 run.")
    parser.add_option("--dist-server-name",
                      default="127.0.0.1",
                      action="store", type="string",
                      help="Name of the message server host\nDEFAULT: localhost")
    parser.add_option("--dist-server-port",
                      default=2200,
                      action="store", type="int",
                      help="Message server listen port\nDEFAULT: 2200")
    parser.add_option("--dist-sync-repeat",
                      default="0us",
                      action="store", type="string",
                      help="Repeat interval for synchronisation barriers among dist-gem5 processes\nDEFAULT: --ethernet-linkdelay")
    parser.add_option("--dist-sync-start",
                      default="5200000000000t",
                      action="store", type="string",
                      help="Time to schedule the first dist synchronisation barrier\nDEFAULT:5200000000000t")
    parser.add_option("--ethernet-linkspeed", default="10Gbps",
                        action="store", type="string",
                        help="Link speed in bps\nDEFAULT: 10Gbps")
    parser.add_option("--ethernet-linkdelay", default="10us",
                      action="store", type="string",
                      help="Link delay in seconds\nDEFAULT: 10us")

    # Run duration options
    parser.add_option("-I", "--maxinsts", action="store", type="int",
                      default=None, help="""Total number of instructions to
                                            simulate (default: run forever)""")
    parser.add_option("--work-item-id", action="store", type="int",
                      help="the specific work id for exit & checkpointing")
    parser.add_option("--num-work-ids", action="store", type="int",
                      help="Number of distinct work item types")
    parser.add_option("--work-begin-cpu-id-exit", action="store", type="int",
                      help="exit when work starts on the specified cpu")
    parser.add_option("--work-end-exit-count", action="store", type="int",
                      help="exit at specified work end count")
    parser.add_option("--work-begin-exit-count", action="store", type="int",
                      help="exit at specified work begin count")
    parser.add_option("--init-param", action="store", type="int", default=0,
                      help="""Parameter available in simulation with m5
                              initparam""")
    parser.add_option("--initialize-only", action="store_true", default=False,
                      help="""Exit after initialization. Do not simulate time.
                              Useful when gem5 is run as a library.""")

    # Simpoint options
    parser.add_option("--simpoint-profile", action="store_true",
                      help="Enable basic block profiling for SimPoints")
    parser.add_option("--simpoint-interval", type="int", default=10000000,
                      help="SimPoint interval in num of instructions")
    parser.add_option("--take-simpoint-checkpoints", action="store", type="string",
        help="<simpoint file,weight file,interval-length,warmup-length>")
    parser.add_option("--restore-simpoint-checkpoint", action="store_true",
        help="restore from a simpoint checkpoint taken with " +
             "--take-simpoint-checkpoints")

    # Checkpointing options
    ###Note that performing checkpointing via python script files will override
    ###checkpoint instructions built into binaries.
    parser.add_option("--take-checkpoints", action="store", type="string",
        help="<M,N> take checkpoints at tick M and every N ticks thereafter")
    parser.add_option("--max-checkpoints", action="store", type="int",
        help="the maximum number of checkpoints to drop", default=5)
    parser.add_option("--checkpoint-dir", action="store", type="string",
        help="Place all checkpoints in this absolute directory")
    parser.add_option("-r", "--checkpoint-restore", action="store", type="int",
        help="restore from checkpoint <N>")
    parser.add_option("--checkpoint-at-end", action="store_true",
                      help="take a checkpoint at end of run")
    parser.add_option("--work-begin-checkpoint-count", action="store", type="int",
                      help="checkpoint at specified work begin count")
    parser.add_option("--work-end-checkpoint-count", action="store", type="int",
                      help="checkpoint at specified work end count")
    parser.add_option("--work-cpus-checkpoint-count", action="store", type="int",
                      help="checkpoint and exit when active cpu count is reached")
    parser.add_option("--restore-with-cpu", action="store", type="choice",
                      default="AtomicSimpleCPU", choices=CpuConfig.cpu_names(),
                      help = "cpu type for restoring from a checkpoint")


    # CPU Switching - default switch model goes from a checkpoint
    # to a timing simple CPU with caches to warm up, then to detailed CPU for
    # data measurement
    parser.add_option("--repeat-switch", action="store", type="int",
        default=None,
        help="switch back and forth between CPUs with period <N>")
    parser.add_option("-s", "--standard-switch", action="store", type="int",
        default=None,
        help="switch from timing to Detailed CPU after warmup period of <N>")
    parser.add_option("-p", "--prog-interval", type="str",
        help="CPU Progress Interval")

    # Fastforwarding and simpoint related materials
    parser.add_option("-W", "--warmup-insts", action="store", type="int",
        default=None,
        help="Warmup period in total instructions (requires --standard-switch)")
    parser.add_option("--bench", action="store", type="string", default=None,
        help="base names for --take-checkpoint and --checkpoint-restore")
    parser.add_option("-F", "--fast-forward", action="store", type="string",
        default=None,
        help="Number of instructions to fast forward before switching")
    parser.add_option("-S", "--simpoint", action="store_true", default=False,
        help="""Use workload simpoints as an instruction offset for
                --checkpoint-restore or --take-checkpoint.""")
    parser.add_option("--at-instruction", action="store_true", default=False,
        help="""Treat value of --checkpoint-restore or --take-checkpoint as a
                number of instructions.""")
    parser.add_option("--spec-input", default="ref", type="choice",
                      choices=["ref", "test", "train", "smred", "mdred",
                               "lgred"],
                      help="Input set size for SPEC CPU2000 benchmarks.")
    parser.add_option("--arm-iset", default="arm", type="choice",
                      choices=["arm", "thumb", "aarch64"],
                      help="ARM instruction set.")


def addSEOptions(parser):
    # Benchmark options
    parser.add_option("-c", "--cmd", default="",
                      help="The binary to run in syscall emulation mode.")
    parser.add_option("-o", "--options", default="",
                      help="""The options to pass to the binary, use " "
                              around the entire string""")
    parser.add_option("-e", "--env", default="",
                      help="Initialize workload environment from text file.")
    parser.add_option("-i", "--input", default="",
                      help="Read stdin from a file.")
    parser.add_option("--output", default="",
                      help="Redirect stdout to a file.")
    parser.add_option("--errout", default="",
                      help="Redirect stderr to a file.")

def addFSOptions(parser):
    from FSConfig import os_types

    # Simulation options
    parser.add_option("--timesync", action="store_true",
            help="Prevent simulated time from getting ahead of real time")

    # System options
    parser.add_option("--kernel", action="store", type="string")
    parser.add_option("--os-type", action="store", type="choice",
            choices=os_types[buildEnv['TARGET_ISA']], default="linux",
            help="Specifies type of OS to boot")
    parser.add_option("--script", action="store", type="string")
    parser.add_option("--frame-capture", action="store_true",
            help="Stores changed frame buffers from the VNC server to compressed "\
            "files in the gem5 output directory")

    if buildEnv['TARGET_ISA'] == "arm":
        parser.add_option("--bare-metal", action="store_true",
                   help="Provide the raw system without the linux specific bits")
        parser.add_option("--list-machine-types",
                          action="callback", callback=_listPlatformTypes,
                      help="List available platform types")
        parser.add_option("--machine-type", action="store", type="choice",
                choices=PlatformConfig.platform_names(),
                default="VExpress_EMM")
        parser.add_option("--dtb-filename", action="store", type="string",
              help="Specifies device tree blob file to use with device-tree-"\
              "enabled kernels")
        parser.add_option("--enable-security-extensions", action="store_true",
              help="Turn on the ARM Security Extensions")
        parser.add_option("--enable-context-switch-stats-dump", \
                action="store_true", help="Enable stats dump at context "\
                "switches and dump tasks file (required for Streamline)")
        parser.add_option("--generate-dtb", action="store_true", default=False,
                    help="Automatically generate a dtb file")

    # Benchmark options
    parser.add_option("--dual", action="store_true",
                      help="Simulate two systems attached with an ethernet link")
    parser.add_option("-b", "--benchmark", action="store", type="string",
                      dest="benchmark",
                      help="Specify the benchmark to run. Available benchmarks: %s"\
                      % DefinedBenchmarks)

    # Metafile options
    parser.add_option("--etherdump", action="store", type="string", dest="etherdump",
                      help="Specify the filename to dump a pcap capture of the" \
                      "ethernet traffic")

    # Disk Image Options
    parser.add_option("--disk-image", action="store", type="string", default=None,
                      help="Path to the disk image to use.")
    parser.add_option("--root-device", action="store", type="string", default=None,
                      help="OS device name for root partition")

    # Command line options
    parser.add_option("--command-line", action="store", type="string",
                      default=None,
                      help="Template for the kernel command line.")
    parser.add_option("--command-line-file", action="store",
                      default=None, type="string",
                      help="File with a template for the kernel command line")
# @PIM
def addPIMOptions(parser):
    parser.add_option("--enable-pim", action="store_true")
    parser.add_option("--pim-type", type="choice", default="cpu",
                      choices=["cpu", "kernel", "hybrid"],
		      help = "type of pim kernel inside the memory to use")
    parser.add_option("--kernel-type", type="choice", default="adder",
                      choices=["adder", "multiplier", "divider"],
		      help = "type of pim kernel inside the memory to use")
    parser.add_option("--num-pim-kernels", type="int", default=0,
                  help="Number of in-memory kernels")
    parser.add_option("--num-pim-processors", type="int", default=0,
                  help="Number of in-memory processors")
    parser.add_option("--coherence-granularity", type="string", default="64B")


def add_hmc_options(parser):
    # *****************************CROSSBAR PARAMETERS*************************
    # Flit size of the main interconnect [1]
    parser.add_option("--xbar-width", default=32, action="store", type=int,
                        help="Data width of the main XBar (Bytes)")

    # Clock frequency of the main interconnect [1]
    # This crossbar, is placed on the logic-based of the HMC and it has its
    # own voltage and clock domains, different from the DRAM dies or from the
    # host.
    parser.add_option("--xbar-frequency", default='1GHz', type=str,
                        help="Clock Frequency of the main XBar")

    # Arbitration latency of the HMC XBar [1]
    parser.add_option("--xbar-frontend-latency", default=1, action="store",
                        type=int, help="Arbitration latency of the XBar")

    # Latency to forward a packet via the interconnect [1](two levels of FIFOs
    # at the input and output of the inteconnect)
    parser.add_option("--xbar-forward-latency", default=2, action="store",
                        type=int, help="Forward latency of the XBar")

    # Latency to forward a response via the interconnect [1](two levels of
    # FIFOs at the input and output of the inteconnect)
    parser.add_option("--xbar-response-latency", default=2, action="store",
                        type=int, help="Response latency of the XBar")

    # number of cross which connects 16 Vaults to serial link[7]
    parser.add_option("--number-mem-crossbar", default=4, action="store",
                        type=int, help="Number of crossbar in HMC")

    # *****************************SERIAL LINK PARAMETERS**********************
    # Number of serial links controllers [1]
    parser.add_option("--num-links-controllers", default=4, action="store",
                        type=int, help="Number of serial links")

    # Number of packets (not flits) to store at the request side of the serial
    #  link. This number should be adjusted to achive required bandwidth
    parser.add_option("--link-buffer-size-req", default=10, action="store",
                        type=int, help="Number of packets to buffer at the\
                        request side of the serial link")

    # Number of packets (not flits) to store at the response side of the serial
    #  link. This number should be adjusted to achive required bandwidth
    parser.add_option("--link-buffer-size-rsp", default=10, action="store",
                        type=int, help="Number of packets to buffer at the\
                        response side of the serial link")

    # Latency of the serial link composed by SER/DES latency (1.6ns [4]) plus
    # the PCB trace latency (3ns Estimated based on [5])
    parser.add_option("--link-latency", default='4.6ns', type=str,
                        help="Latency of the serial links")

    # Clock frequency of the each serial link(SerDes) [1]
    parser.add_option("--link-frequency", default='10GHz', type=str,
                        help="Clock Frequency of the serial links")

    # Clock frequency of serial link Controller[6]
    # clk_hmc[Mhz]= num_lanes_per_link * lane_speed [Gbits/s] /
    # data_path_width * 10^6
    # clk_hmc[Mhz]= 16 * 10 Gbps / 256 * 10^6 = 625 Mhz
    parser.add_option("--link-controller-frequency", default='625MHz',
                        type=str, help="Clock Frequency of the link\
                        controller")

    # Latency of the serial link controller to process the packets[1][6]
    # (ClockDomain = 625 Mhz )
    # used here for calculations only
    parser.add_option("--link-ctrl-latency", default=4, action="store",
                        type=int, help="The number of cycles required for the\
                        controller to process the packet")

    # total_ctrl_latency = link_ctrl_latency + link_latency
    # total_ctrl_latency = 4(Cycles) * 1.6 ns +  4.6 ns
    parser.add_option("--total-ctrl-latency", default='11ns', type=str,
                        help="The latency experienced by every packet\
                        regardless of size of packet")

    # Number of parallel lanes in each serial link [1]
    parser.add_option("--num-lanes-per-link", default=16, action="store",
                        type=int, help="Number of lanes per each link")

    # Number of serial links [1]
    parser.add_option("--num-serial-links", default=4, action="store",
                        type=int, help="Number of serial links")

    # speed of each lane of serial link - SerDes serial interface 10 Gb/s
    parser.add_option("--serial-link-speed", default=10, action="store",
                        type=int, help="Gbs/s speed of each lane of serial\
                        link")

    # address range for each of the serial links
    parser.add_option("--serial-link-addr-range", default='1GB', type=str,
                        help="memory range for each of the serial links.\
                        Default: 1GB")

    # *****************************PERFORMANCE MONITORING*********************
    # The main monitor behind the HMC Controller
    parser.add_option("--enable-global-monitor", action="store_true",
                        help="The main monitor behind the HMC Controller")

    # The link performance monitors
    parser.add_option("--enable-link-monitor", action="store_true",
                        help="The link monitors")

    # link aggregator enable - put a cross between buffers & links
    parser.add_option("--enable-link-aggr", action="store_true", help="The\
                        crossbar between port and Link Controller")

    parser.add_option("--enable-buff-div", action="store_true",
                        help="Memory Range of Buffer is ivided between total\
                        range")

    # *****************************HMC ARCHITECTURE **************************
    # Memory chunk for 16 vault - numbers of vault / number of crossbars
    parser.add_option("--mem-chunk", default=4, action="store", type=int,
                        help="Chunk of memory range for each cross bar in\
                        arch 0")

    # size of req buffer within crossbar, used for modelling extra latency
    # when the reuqest go to non-local vault
    parser.add_option("--xbar-buffer-size-req", default=10, action="store",
                        type=int, help="Number of packets to buffer at the\
                        request side of the crossbar")

    # size of response buffer within crossbar, used for modelling extra latency
    # when the response received from non-local vault
    parser.add_option("--xbar-buffer-size-resp", default=10, action="store",
                        type=int, help="Number of packets to buffer at the\
                        response side of the crossbar")
    # HMC device architecture. It affects the HMC host controller as well
    parser.add_option("--arch", type="choice", choices=["same", "distributed",
                        "mixed"], default="distributed", help="same: HMC with\
                        4 links, all with same range.\ndistributed: HMC with\
                        4 links with distributed range.\nmixed: mixed with\
                        same and distributed range.\nDefault: distributed")
    # HMC device - number of vaults
    parser.add_option("--hmc-dev-num-vaults", default=16, action="store",
                        type=int, help="number of independent vaults within\
                        the HMC device. Note: each vault has a memory\
                        controller (valut controller)\nDefault: 16")
    # HMC device - vault capacity or size
    parser.add_option("--hmc-dev-vault-size", default='256MB', type=str,
                        help="vault storage capacity in bytes. Default:\
                        256MB")
    #parser.add_option("--mem-type", type=str, choices=["HMC_2500_1x32"],
    #                    default="HMC_2500_1x32", help="type of HMC memory to\
    #                    use. Default: HMC_2500_1x32")
    #parser.add_option("--mem-channels", default=1, action="store", type=int,
    #                    help="Number of memory channels")
    #parser.add_option("--mem-ranks", default=1, action="store", type=int,
    #                    help="Number of ranks to iterate across")
    parser.add_option("--burst-length", default=256, action="store",
                        type=int, help="burst length in bytes. Note: the\
                        cache line size will be set to this value.\nDefault:\
                        256")


