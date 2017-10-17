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

#include <cassert>

/**
 * When building the debug binary, we need to undo the command-line
 * definition of DEBUG not to clash with DRAMSim2 print macros that
 * are included for no obvious reason.
 */
#ifdef DEBUG
#undef DEBUG
#endif

#include "mem/hmcsim_wrapper.hh"
//#include "HMCSim/hmc_sim_macros.h"
//#include "HMCSim/hmc_sim_types.h"
#include <fstream>

#include "base/compiler.hh"
#include "base/misc.hh"

/**
 * DRAMSim2 requires SHOW_SIM_OUTPUT to be defined (declared extern in
 * the DRAMSim2 print macros), otherwise we get linking errors due to
 * undefined references
 */


HMCSimWrapper::HMCSimWrapper(
                             uint32_t num_devs,
                             uint32_t num_links,
                             uint32_t num_vaults,
                             uint32_t queue_depth,
                             uint32_t num_banks,
                             uint32_t num_drams,
                             uint32_t capacity,
                             uint32_t xbar_depth) :
hmcsim(new hmcsim_t()),
_clockPeriod(0.0), _queueSize(0), _burstSize(0),
_num_devs (num_devs),
_num_links (num_links),
_num_vaults (num_vaults),
_queue_depth (queue_depth),
_num_banks (num_banks),
_num_drams (num_drams),
_capacity (capacity),
_xbar_depth (xbar_depth)


{
    
    // switch on debug output if requested
    //if (enable_debug)
     //   SHOW_SIM_OUTPUT = 1;
    
    // there is no way of getting HMCSim to tell us what frequency
    // it is assuming, so we have to extract it ourselves
    _clockPeriod = 1.0;
    
    if (!_clockPeriod)
        fatal("HMCSim wrapper failed to get clock\n");
    
    // we also need to know what transaction queue size DRAMSim2 is
    // using so we can stall when responses are blocked
    _queueSize = queue_depth;
    
    if (!_queueSize)
        fatal("HMCSim wrapper failed to get queue size\n");
    rtns = (int*)malloc( sizeof( int ) * num_links );
    memset( rtns, 0, sizeof( int ) * num_links );

    
    ret = hmcsim_init(	hmcsim,
                      _num_devs,
                      _num_links,
                      _num_vaults,
                      _queue_depth,
                      _num_banks,
                      _num_drams,
                      _capacity,
                      _xbar_depth );
    if( ret != 0 ){
        panic( "FAILED TO INIT HMCSIM\n" );

    }else {
        printf( "SUCCESS : INITALIZED HMCSIM\n" );
    }
    
    /*
     * set the maximum request size for all devices
     *
     */
    ret = hmcsim_util_set_all_max_blocksize( hmcsim, 128 );
    if( ret != 0 ){ 
        
        hmcsim_free( hmcsim );
        panic( "FAILED TO SET MAXIMUM BLOCK SIZE\n" );
    }else {
        printf( "SUCCESS : SET MAXIMUM BLOCK SIZE\n" );
    }

}

HMCSimWrapper::~HMCSimWrapper()
{
    hmcsim_free( hmcsim );
}


void
HMCSimWrapper::printStats()
{
    hmcsim_trace_header(hmcsim);
}



bool
HMCSimWrapper::canAccept() const
{
    //return dramsim->willAcceptTransaction();
    return ret!=HMC_STALL;
}

void
HMCSimWrapper::enqueue(PacketPtr pkt, uint64_t addr)
{
   // bool success M5_VAR_USED = dramsim->addTransaction(is_write, addr);
   // assert(success);
    QueuedReqs[addr].push(pkt);
    
}

double
HMCSimWrapper::clockPeriod() const
{
    return _clockPeriod;
}

unsigned int
HMCSimWrapper::queueSize() const
{
    return _queueSize;
}

unsigned int
HMCSimWrapper::burstSize() const
{
    return _burstSize;
}
void
HMCSimWrapper::zero_packet( uint64_t *packet )
{
    uint64_t i = 0x00ll;
    
    /*
     * zero the packet
     *
     */
    for( i=0; i<HMC_MAX_UQ_PACKET; i++ ){
        packet[i] = 0x00ll;
    } 
    
    
    return ;
}
void
HMCSimWrapper::setCallbacks(DRAMSim::TransactionCompleteCB* read_callback,
                              DRAMSim::TransactionCompleteCB* write_callback)
{
    // simply pass it on, for now we ignore the power callback
    ReturnReadData=read_callback;
    WriteDataDone=write_callback;
}

void
HMCSimWrapper::tick()
{
    if(QueuedReqs.size()<=0){
        hmcsim_clock(hmcsim);
        return;
    }
     ret = HMC_OK;
    while(ret!=HMC_STALL&&QueuedReqs.size()>0){
        auto first =QueuedReqs.begin();
        PacketPtr firstpkt = first->second.front();
        if(firstpkt->isRead()){
            hmcsim_build_memrequest( hmcsim,
                                0,
                                firstpkt->getAddr()-_offset,
                                tag,
                                RD64,
                                link,
                                &(payload[0]),
                                &head,
                                &tail );
            packet[0] = head;
            packet[1] = tail;
        
        }else{
            if(firstpkt->isWrite()){
            hmcsim_build_memrequest( hmcsim,
                                    0,
                                    firstpkt->getAddr()-_offset,
                                    tag,
                                    WR64,
                                    link,
                                    &(payload[0]),
                                    &head,
                                    &tail );

            packet[0] = head;
            packet[1] = 0x05ll;
            packet[2] = 0x06ll;
            packet[3] = 0x07ll;
            packet[4] = 0x08l;
            packet[5] = 0x09ll;
            packet[6] = 0x0All;
            packet[7] = 0x0Bll;
            packet[8] = 0x0Cll;
            packet[9] = tail;


            }else{
                panic("eroor");
                hmcsim_clock(hmcsim);
            }
        
        }
        ret = hmcsim_send( hmcsim, &(packet[0]) );
        switch( ret ){
            case 0:
                printf( "SUCCESS : PACKET WAS SUCCESSFULLY SENT\n" );
                
                OutstandingReqs[tag].push(firstpkt->getAddr());
            {
                auto p = QueuedReqs.find(firstpkt->getAddr());
                assert(p != QueuedReqs.end());
                p->second.pop();
                if (p->second.empty())
                    QueuedReqs.erase(p);
            }
                break;
            case HMC_STALL:
                printf( "STALLED : PACKET WAS STALLED IN SENDING\n" );
                
                break;
            case -1:
            default:
                panic( "FAILED : PACKET SEND FAILED\n" );
                break;
        }

        
        /*
         * zero the packet
         *
         */
        zero_packet( &(packet[0]) );
        tag++;
        if( tag == 2048 ){
            tag = 0;
        }
    
        link++;
        if( link == hmcsim->num_links ){
        /* -- TODO : look at the number of connected links
         * to the host processor
         */
            link = 0;
        }
    }
    
    /*
     * reset the return code for receives
     *
     */
    ret = HMC_OK;
    
    /*
     * We hit a stall or an error
     *
     * Try to drain the responses off all the links
     * 
     */
    uint64_t d_response_head;
    uint64_t d_response_tail;
    hmc_response_t d_type;
    uint8_t d_length;
    uint16_t d_tag;
    uint8_t d_rtn_tag;
    uint8_t d_src_link;
    uint8_t d_rrp;
    uint8_t d_frp;
    uint8_t d_seq;
    uint8_t d_dinv;
    uint8_t d_errstat;
    uint8_t d_rtc;
    uint32_t d_crc;
    while( ret != HMC_STALL ){
        
        for(int z=0; z<hmcsim->num_links; z++){
            
            rtns[z] = hmcsim_recv( hmcsim, 0, z, &(packet[0]) );
            
            if( rtns[z] == HMC_STALL ){
                stall_sig++;
            }else{
                /* successfully received a packet */
                printf( "SUCCESS : RECEIVED A SUCCESSFUL PACKET RESPONSE\n" );
                hmcsim_decode_memresponse( hmcsim,
                                          &(packet[0]),
                                          &d_response_head,
                                          &d_response_tail,
                                          &d_type,
                                          &d_length,
                                          &d_tag,
                                          &d_rtn_tag,
                                          &d_src_link,
                                          &d_rrp,
                                          &d_frp,
                                          &d_seq,
                                          &d_dinv,
                                          &d_errstat,
                                          &d_rtc,
                                          &d_crc );
                printf( "RECV tag=%d; rtn_tag=%d\n", d_tag, d_rtn_tag );
                {
                auto p = OutstandingReqs.find(d_tag);
                assert(p != OutstandingReqs.end());
                
                // first in first out, which is not necessarily true, but it is
                // the best we can do at this point
                    switch(d_type){
                        case WR_RS:
                            (*WriteDataDone)(0,p->second.front(),0);

                            break;
                        case RD_RS:
                            (*ReturnReadData)(0,p->second.front(),0);

                            break;
                        default:
                            panic("ss");
                            break;
                    }
                    p->second.pop();
                    
                    if (p->second.empty())
                        OutstandingReqs.erase(p);
                }

            }
            
            /*
             * zero the packet
             *
             */
            zero_packet( &(packet[0]) );
        }
        
        /* count the number of stall signals received */
        if( stall_sig == hmcsim->num_links ){
            /*
             * if all links returned stalls,
             * then we're done receiving packets
             *
             */
            
            printf( "STALLED : STALLED IN RECEIVING\n" );
            ret = HMC_STALL;
        }
        
        stall_sig = 0;
        for(int z=0; z<hmcsim->num_links; z++){
            rtns[z] = HMC_OK;
        }
    }
    
    
    hmcsim_clock(hmcsim);
}
