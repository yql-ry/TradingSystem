﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Oybab.ServerManager.Model.Service.SupplierPay
{

    public class ToClientServiceGetSupplierPay : ToClientService
    {

        public bool Result { get; set; }

        public string SupplierPays { get; set; }

        public string ImportPays { get; set; }
    }
}
