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
    
    public partial class Room
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Room()
        {
            this.tb_device = new HashSet<Device>();
            this.tb_order = new HashSet<Order>();
        }
    
        public long RoomId { get; set; }
        public string RoomNo { get; set; }
        public long RoomType { get; set; }
        public string RoomTypeName { get; set; }
        public double Price { get; set; }
        public long IsPayByTime { get; set; }
        public double PriceHour { get; set; }
        public double FreeRoomPriceLimit { get; set; }
        public long IsAutoExtendTime { get; set; }
        public long DeviceOfCount { get; set; }
        public long HideType { get; set; }
        public long Order { get; set; }
        public long AddTime { get; set; }
        public Nullable<long> UpdateTime { get; set; }
        public string Remark { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Device> tb_device { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Order> tb_order { get; set; }
    }
}