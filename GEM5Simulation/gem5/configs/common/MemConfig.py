# Copyright (c) 2013, 2017 ARM Limited
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
# Authors: Andreas Sandberg
#          Andreas Hansson

from __future__ import print_function

import m5
from m5.objects import *
from m5.util import *
import inspect
import sys
import HMC
from textwrap import  TextWrapper

# Dictionary of mapping names of real memory controller models to
# classes.
_mem_classes = {}

def is_mem_class(cls):
    """Determine if a class is a memory controller that can be instantiated"""

    # We can't use the normal inspect.isclass because the ParamFactory
    # and ProxyFactory classes have a tendency to confuse it.
    try:
        return issubclass(cls, m5.objects.AbstractMemory) and \
            not cls.abstract
    except TypeError:
        return False

def get(name):
    """Get a memory class from a user provided class name."""

    try:
        mem_class = _mem_classes[name]
        return mem_class
    except KeyError:
        print("%s is not a valid memory controller." % (name,))
        sys.exit(1)

def print_mem_list():
    """Print a list of available memory classes."""

    print("Available memory classes:")
    doc_wrapper = TextWrapper(initial_indent="\t\t", subsequent_indent="\t\t")
    for name, cls in _mem_classes.items():
        print("\t%s" % name)

        # Try to extract the class documentation from the class help
        # string.
        doc = inspect.getdoc(cls)
        if doc:
            for line in doc_wrapper.wrap(doc):
                print(line)

def mem_names():
    """Return a list of valid memory names."""
    return _mem_classes.keys()

# Add all memory controllers in the object hierarchy.
for name, cls in inspect.getmembers(m5.objects, is_mem_class):
    _mem_classes[name] = cls

def create_mem_ctrl(cls, r, i, nbr_mem_ctrls, intlv_bits, intlv_size):
    """
    Helper function for creating a single memoy controller from the given
    options.  This function is invoked multiple times in config_mem function
    to create an array of controllers.
    """

	
    import math
    intlv_low_bit = int(math.log(intlv_size, 2))

    # Use basic hashing for the channel selection, and preferably use
    # the lower tag bits from the last level cache. As we do not know
    # the details of the caches here, make an educated guess. 4 MByte
    # 4-way associative with 64 byte cache lines is 6 offset bits and
    # 14 index bits.
    xor_low_bit = 20

    # Create an instance so we can figure out the address
    # mapping and row-buffer size
    ctrl = cls()

    # Only do this for DRAMs
    if issubclass(cls, m5.objects.DRAMCtrl):
        # Inform each controller how many channels to account
        # for
        ctrl.channels = nbr_mem_ctrls

        # If the channel bits are appearing after the column
        # bits, we need to add the appropriate number of bits
        # for the row buffer size
        if ctrl.addr_mapping.value == 'RoRaBaChCo':
            # This computation only really needs to happen
            # once, but as we rely on having an instance we
            # end up having to repeat it for each and every
            # one
            rowbuffer_size = ctrl.device_rowbuffer_size.value * \
                ctrl.devices_per_rank.value

            intlv_low_bit = int(math.log(rowbuffer_size, 2))


	    

    # We got all we need to configure the appropriate address
    # range
    ctrl.range = m5.objects.AddrRange(r.start, size = r.size(),
                                      intlvHighBit = \
                                          intlv_low_bit + intlv_bits - 1,
                                      xorHighBit = \
                                          xor_low_bit + intlv_bits - 1,
                                      intlvBits = intlv_bits,
                                      intlvMatch = i)
    return ctrl

def config_mem(options, system):
    """
    Create the memory controllers based on the options and attach them.

    If requested, we make a multi-channel configuration of the
    selected memory controller class by creating multiple instances of
    the specific class. The individual controllers have their
    parameters set such that the address range is interleaved between
    them.
    """

    # Mandatory options
    opt_mem_type = options.mem_type
    opt_mem_channels = options.mem_channels

    # Optional options
    opt_tlm_memory = getattr(options, "tlm_memory", None)
    opt_external_memory_system = getattr(options, "external_memory_system",
                                         None)
    opt_elastic_trace_en = getattr(options, "elastic_trace_en", False)
    opt_mem_ranks = getattr(options, "mem_ranks", None)

    if opt_mem_type == "HMC_2500_1x32":
        HMChost = HMC.config_hmc_host_ctrl(options, system)
        HMC.config_hmc_dev(options, system, HMChost.hmc_host)
        subsystem = system.hmc_dev
        xbar = system.hmc_dev.xbar
	
    else:
        subsystem = system
        xbar = system.membus

    #system.mem_ranges.add



    if opt_tlm_memory:
        system.external_memory = m5.objects.ExternalSlave(
            port_type="tlm_slave",
            port_data=opt_tlm_memory,
            port=system.membus.master,
            addr_ranges=system.mem_ranges)
        system.kernel_addr_check = False
        return

    if opt_external_memory_system:
        subsystem.external_memory = m5.objects.ExternalSlave(
            port_type=opt_external_memory_system,
            port_data="init_mem0", port=xbar.master,
            addr_ranges=system.mem_ranges)
        subsystem.kernel_addr_check = False
        return

    nbr_mem_ctrls = opt_mem_channels
    import math
    from m5.util import fatal
    intlv_bits = int(math.log(nbr_mem_ctrls, 2))
    if 2 ** intlv_bits != nbr_mem_ctrls:
        fatal("Number of memory channels must be a power of 2")

    cls = get(opt_mem_type)
    mem_ctrls = []

    if opt_elastic_trace_en and not issubclass(cls, m5.objects.SimpleMemory):
        fatal("When elastic trace is enabled, configure mem-type as "
                "simple-mem.")

    # The default behaviour is to interleave memory channels on 128
    # byte granularity, or cache line granularity if larger than 128
    # byte. This value is based on the locality seen across a large
    # range of workloads.
    intlv_size = max(128, system.cache_line_size.value)

    # For every range (most systems will only have one), create an
    # array of controllers and set their parameters to match their
    # address mapping in the case of a DRAM

    # @PIM
    # if we use PIM, we should get the memory ranges in order to 
    # differentiate phyical memory and in-memory logic/processors
    addr_base = 0


    for r in system.mem_ranges:
        for i in xrange(nbr_mem_ctrls):
            mem_ctrl = create_mem_ctrl(cls, r, i, nbr_mem_ctrls, intlv_bits,
                                       intlv_size)
            # Set the number of ranks based on the command-line
            # options if it was explicitly set
            if issubclass(cls, m5.objects.DRAMCtrl) and opt_mem_ranks:
                mem_ctrl.ranks_per_channel = opt_mem_ranks

            if opt_elastic_trace_en:
                mem_ctrl.latency = '1ns'
                print("For elastic trace, over-riding Simple Memory "
                    "latency to 1ns.")
	    if hasattr(options,'enable_pim') and options.enable_pim:
	        mem_ctrl.cpu_type = options.cpu_type
		mem_ctrl.coherence_granularity=options.coherence_granularity
            mem_ctrls.append(mem_ctrl)

	    # @PIM
	    # If the memory consists of more than two controller, the ranges
	    # may be separated. It is Thus, we should find the 
	    if long(r.end)> addr_base:
		addr_base = r.end

    subsystem.mem_ctrls = mem_ctrls
    if options.mem_type.startswith("HMC"):
	print("xxx")
    	addr_base = int(MemorySize(options.hmc_dev_vault_size))*options.hmc_dev_num_vaults  -1
        print(addr_base)
    # @PIM 
    # define in-memory processing units here
    addr_base = addr_base + 1
    if(hasattr(options,'enable_pim')):
	pim_enable = options.enable_pim
    if hasattr(options,'enable_pim') and pim_enable:
        print ("Enable PIM simulation in the system.")

        pim_type = options.pim_type
        num_kernels = options.num_pim_kernels
        num_processors = options.num_pim_processors
        num_pim_logic = num_kernels + num_processors

        if num_pim_logic <= 0:
            fatal ("The num of PIM logic/processors cannot be zero while enabling PIM.")
	if options.mem_type.startswith("HMC"):
	    if num_kernels>0:
		num_kernels=16
		num_processors=0
	    else:
		num_processors=16
		num_kernels=0
        system.pim_type = pim_type
        for cpu in system.cpu:
	    # let host-side processors know the address of PIM logic
            cpu.pim_base_addr = addr_base

	    # memory contains kernels
        if pim_type != "cpu" and num_kernels > 0:
            pim_kernerls = []
	
            print ("Creating PIM kernels...")
            for pid in range(num_kernels):
		if(options.kernel_type=="adder"):
                    _kernel = PIMAdder()
		else:
		    if(options.kernel_type=="multiplier"):
			_kernel = PIMMultiplier()
		    else:
			if(options.kernel_type=="divider"):
			    _kernel = PIMDivider()
			else:
			    fatal("no pim kernel type specified.")
                vd = VoltageDomain(voltage="1.0V")
                _kernel.clk_domain = SrcClockDomain(clock="1GHz", voltage_domain=vd)
                _kernel.id = pid

		# Currently, we use only one bit for accessing a PIM kernel.
		# Detailed PIM information is defined inside the packet
		# at mem/pactet.hh(cc) 
                _kernel.addr_ranges = AddrRange(addr_base + pid, addr_base + pid)
                _kernel.addr_base = addr_base

                if options.mem_type.startswith("DDR"):
		    # connect to the memory bus if the memory is DRAM
                    _kernel.port = xbar.slave
                    _kernel.mem_port = xbar.master
		if options.mem_type.startswith("HMC"):
        	    _kernel.port = system.membus.slave
            	    _kernel.mem_port = system.membus.master
                pim_kernerls.append(_kernel)
            system.pim_kernerls = pim_kernerls

	# memory contains processors

        if pim_type != "kernel" and num_processors > 0:

            system.pim_cpu = TimingSimpleCPU( ispim =True, total_host_cpu = options.num_cpus, switched_out =True) 
            pim_vd = VoltageDomain(voltage="1.0V")
            system.pim_cpu.clk_domain = SrcClockDomain(clock = '1GHz', voltage_domain = pim_vd)
            print ("Creating PIM processors...")

	    system.pim_cpu.icache_port = system.membus.slave
	    system.pim_cpu.dcache_port = system.membus.slave
	    system.pim_cpu.workload = system.cpu[0].workload[0]

	    system.pim_cpu.isa = [ default_isa_class()]


        if pim_type == "hybrid":
            if (num_kernels >0 and num_processors > 0) == False:
                fatal ("PIM logic is set to hybrid without configured")
		
	   



    # Connect the controllers to the membus
    for i in xrange(len(subsystem.mem_ctrls)):
        if opt_mem_type == "HMC_2500_1x32":
            subsystem.mem_ctrls[i].port = xbar[i/4].master

            # Set memory device size. There is an independent controller for
            # each vault. All vaults are same size.
            subsystem.mem_ctrls[i].device_size = options.hmc_dev_vault_size
        else:
            subsystem.mem_ctrls[i].port = xbar.master
