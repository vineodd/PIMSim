#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.General;

#endregion

namespace PIMSim.Procs
{
    /// <summary>
    /// Computing Stage
    /// </summary>
    public class Stage_Computing : Stage
    {
        #region Private Variables

        private int add_ability;
        private int multi_ability;
        private int add_ = 0;
        private int multi_ = 0;

        #endregion

        #region Public Methods

 

        /// <summary>
        /// set stage input
        /// </summary>
        /// <param name="obj"></param>
        public override void set_input(object obj)
        {
            if (last != null)
            {
                if (last[0].output_ready)
                {
                    var obj_ = new Instruction() as object;
                    last[0].get_output(ref obj_);
                    input = obj_ as Instruction;
                    input_ready = true;
                }
            }
        }

        public void set_multi_counter(int multi_)
        {
            multi_ability = multi_;
        }
        public void set_add_counter(int add_)
        {
            add_ability = add_;
        }

        /// <summary>
        /// internal step
        /// </summary>
        /// <returns></returns>
        public override bool Step()
        {
            stall = false;
            if (add_ != 0 || multi_ != 0)
            {
                bool add = false;
        bool multi = false;
                while (true)
                {
                    if (add_ <= add_ability)
                    {
                        add = true;
                    }
                    else
                    {
                        add_ -= add_ability;
                    }
                    if (multi_ <= multi_ability)
                    {
                        multi = true;
                    }
                    else
                    {
                        multi_ -= multi_ability;
                    }
                    if (add && multi)
                    {
                        add_ = multi_ = 0;
                        write_output();
                        return true;
                    }
                    return false;

                }
            }
            else
            {
                //first time the ins been handled
                set_input(null);
                if (!read_input())
                    return false;
                if ((intermid as Instruction).type == InstructionType.NOP)
                {
                    write_output();
                    return true;
                }
                string op = (intermid as Instruction).Operation;

                if (Cal_table.ContainOPs(op))
                {
                    //found entry
                    var item = Cal_table.GetItem(op);
add_ = item.Item2;
                    multi_ = item.Item3;

                }
                else
                {
                    add_ = 1;
                    multi_ = 0;
                }
                bool add = false;
bool multi = false;
                while (true)
                {
                    if (add_ <= add_ability)
                    {
                        add = true;
                    }
                    else
                    {
                        add_ -= add_ability;
                    }
                    if (multi_ <= multi_ability)
                    {
                        multi = true;
                    }
                    else
                    {
                        multi_ -= multi_ability;
                    }
                    if (add && multi)
                    {
                        add_ = multi_ = 0;
                        write_output();
                        return true;

                    }
                    else
                    {
                        return false; 
                    }

                }


            }
        }
        //public override bool Step()
        //{

        //    if (add_ != 0 || multi_ != 0)
        //    {
        //        int count_multi = 0;
        //        int count_add = 0;
        //        while (true)
        //        {
        //            bool res = true;
        //            int tp = add_;
        //            for (int i = 0; i < tp; i++)
        //            {
        //                res = add.WaitOne();
        //                if (res)
        //                    add_--;
        //                count_add++;
        //            }
        //            if (!res)
        //            {
        //                for (int i = 0; i < count_add; i++)
        //                    add.Release();

        //            }
        //            tp = multi_;
        //            bool res_ = true;
        //            for (int i = 0; i < tp; i++)
        //            {
        //                res_ = multi.WaitOne();
        //                if (res_)
        //                    multi_--;
        //                count_multi++;
        //            }
        //            if (!res_)
        //            {
        //                for (int i = 0; i < count_multi; i++)
        //                    multi.Release();
        //            }
        //            if (res && res_)
        //            {

        //                write_output();
        //                return true;
        //            }
        //            return false;
        //        }
        //    }
        //    else
        //    {
        //        //first time the ins been handled
        //        set_input(null);
        //        if (!read_input())
        //            return true;
        //        if ((intermid as Instruction).type == InstructionType.NOP)
        //        {
        //            write_output();
        //            return true;
        //        }
        //        string op = (intermid as Instruction).Operation;

        //        if (Cal_table.ContainOPs(op))
        //        {
        //            //found entry
        //            var item = Cal_table.GetItem(op);
        //            add_ = item.Item2;
        //            multi_ = item.Item3;

        //        }
        //        else
        //        {
        //            add_ = 1;
        //            multi_ = 0;
        //        }
        //        int count_multi = 0;
        //        int count_add = 0;
        //        while (true)
        //        {
        //            bool res = true;
        //            int tp = add_;
        //            for (int i = 0; i < tp; i++)
        //            {
        //                res = add.WaitOne();
        //                if (res)
        //                    add_--;
        //                count_add++;
        //            }
        //            if (!res)
        //            {
        //                for (int i = 0; i < count_add; i++)
        //                    add.Release();

        //            }
        //            tp = multi_;
        //            bool res_ = true;
        //            for (int i = 0; i < tp; i++)
        //            {
        //                res_ = multi.WaitOne();
        //                if (res_)
        //                    multi_--;
        //                count_multi++;
        //            }
        //            if (!res_)
        //            {
        //                for (int i = 0; i < count_multi; i++)
        //                    multi.Release();
        //            }
        //            if (res && res_)
        //            {
        //                write_output();
        //                return true;
        //            }
        //            return false;
        //        }

        //    }
        //}


        /// <summary>
        /// read input
        /// </summary>
        /// <returns></returns>
        public override bool read_input()
        {
            if (input_ready && input != null)
            {
                intermid = input;
                input_ready = false;
                input = null;
                return true;
            }
            return false;
        }

        #endregion
    }
}
