/*
 * Copyright (c) 2013 ARM Limited
 * All rights reserved
 *
 * The license below extends only to copyright in the software and shall
 * not be construed as granting a license to any other intellectual
 * property including but not limited to intellectual property relating
 * to a hardware implementation of the functionality of the software
 * licensed hereunder.  You may use the software subject to the license
 * terms below provided that you ensure that this notice is replicated
 * unmodified and in its entirety in all distributions of the software,
 * modified or unmodified, in source code or in binary form.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are
 * met: redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer;
 * redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution;
 * neither the name of the copyright holders nor the names of its
 * contributors may be used to endorse or promote products derived from
 * this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
 * OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
 * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * Authors: Andreas Hansson
 */

/**
 * @file
 * DRAMSim2Wrapper declaration
 */

#ifndef __PIM_HMCSIM__WRAPPER_HH__
#define __PIM_HMCSIM__WRAPPER_HH__

#include <string>
#include <queue>
#include <unordered_map>
#include "sim/sim_object.hh"
#include "mem/qport.hh"
#include "HMCSim/hmc_sim.h"
#include "DRAMSim2/Callback.h"
using namespace std;
/**
 * Forward declaration to avoid includes
 */
//namespace HMCSim {
    
  //  struct hmcsim_t hmc;
    
//}

/**
 * Wrapper class to avoid having DRAMSim2 names like ClockDomain etc
 * clashing with the normal gem5 world. Many of the DRAMSim2 headers
 * do not make use of namespaces, and quite a few also open up
 * std. The only thing that needs to be exposed externally are the
 * callbacks. This wrapper effectively avoids clashes by not including
 * any of the conventional gem5 headers (e.g. Packet or SimObject).
 */
class HMCSimWrapper
{
    
private:
    typedef DRAMSim::CallbackBase<void,unsigned,uint64_t,uint64_t> Callback_t;
    
    hmcsim_t* hmcsim;
    
    double _clockPeriod;
    
    unsigned int _queueSize;
    
    unsigned int _burstSize;
    
    
    uint32_t _num_devs;
    
    uint32_t _num_links;
    
    uint32_t _num_vaults;
    
    uint32_t _queue_depth;
    
    uint32_t _num_banks;
    
    uint32_t _num_drams;
    
    uint32_t _capacity;
    
    uint32_t _xbar_depth;
    

    
    int ret = HMC_OK;
    uint8_t link		= 0;
    uint64_t head		= 0x00ll;
    uint64_t tail		= 0x00ll;
    uint64_t payload[8]	= {0x00ll,0x00ll,0x00ll,0x00ll,
        0x00ll,0x00ll,0x00ll,0x00ll};
    uint16_t tag		= 0;
    uint64_t packet[HMC_MAX_UQ_PACKET];
    int *rtns		= NULL;
    int stall_sig		= 0;
    void zero_packet( uint64_t *packet );
public:
        uint64_t _offset;
    std::unordered_map<Addr, std::queue<PacketPtr> > QueuedReqs;
    std::unordered_map<Addr, std::queue<uint16_t> > OutstandingReqs;

    HMCSimWrapper(
                  uint32_t num_devs,
                  uint32_t num_links,
                  uint32_t num_vaults,
                  uint32_t queue_depth,
                  uint32_t num_banks,
                  uint32_t num_drams,
                  uint32_t capacity, 
                  uint32_t xbar_depth);
    ~HMCSimWrapper();
    

    void printStats();
    
    /**
     * Set the callbacks to use for read and write completion.
     *
     * @param read_callback Callback used for read completions
     * @param write_callback Callback used for write completions
     */
    void setCallbacks(DRAMSim::TransactionCompleteCB* read_callback,
                      DRAMSim::TransactionCompleteCB* write_callback);
    
    /**
     * Determine if the controller can accept a new packet or not.
     *
     * @return true if the controller can accept transactions
     */
    bool canAccept() const;
    Callback_t* ReturnReadData;
    Callback_t* WriteDataDone;
    /**
     * Enqueue a packet. This assumes that canAccept has returned true.
     *
     * @param pkt Packet to turn into a DRAMSim2 transaction
     */
    void enqueue(PacketPtr pkt, uint64_t addr);
    
    /**
     * Get the internal clock period used by DRAMSim2, specified in
     * ns.
     *
     * @return The clock period of the DRAM interface in ns
     */
    double clockPeriod() const;
    
    /**
     * Get the transaction queue size used by DRAMSim2.
     *
     * @return The queue size counted in number of transactions
     */
    unsigned int queueSize() const;
    
    /**
     * Get the burst size in bytes used by DRAMSim2.
     *
     * @return The burst size in bytes (data width * burst length)
     */
    unsigned int burstSize() const;
    
    /**
     * Progress the memory controller one cycle
     */
    void tick();
    
    
};

#endif
