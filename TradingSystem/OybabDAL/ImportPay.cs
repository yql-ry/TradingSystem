//------------------------------------------------------------------------------
// <auto-generated>
//     此代码已从模板生成。
//
//     手动更改此文件可能导致应用程序出现意外的行为。
//     如果重新生成代码，将覆盖对此文件的手动更改。
// </auto-generated>
//------------------------------------------------------------------------------

namespace Oybab.DAL
{
    using System;
    using System.Collections.Generic;
    
    public partial class ImportPay
    {
        public long ImportPayId { get; set; }
        public long ImportId { get; set; }
        public Nullable<long> BalanceId { get; set; }
        public Nullable<long> SupplierId { get; set; }
        public double OriginalPrice { get; set; }
        public double Rate { get; set; }
        public double RemovePrice { get; set; }
        public double Price { get; set; }
        public double BalancePrice { get; set; }
        public long AdminId { get; set; }
        public long DeviceId { get; set; }
        public long Mode { get; set; }
        public long State { get; set; }
        public string Remark { get; set; }
        public long AddTime { get; set; }
        public Nullable<long> UpdateTime { get; set; }
    
        public virtual Admin tb_admin { get; set; }
        public virtual Balance tb_balance { get; set; }
        public virtual Device tb_device { get; set; }
        public virtual Import tb_import { get; set; }
        public virtual Supplier tb_supplier { get; set; }
    }
}
