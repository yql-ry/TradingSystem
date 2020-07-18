﻿using Oybab.Res.Exceptions;
using Oybab.Res.Server.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oybab.Res.Tools;
using Oybab.DAL;
using Oybab.Res.Server;
using Newtonsoft.Json;
using Oybab.TradingSystemX;
using Oybab.ServerManager.Model.Service.Common;
using Oybab.ServerManager.Model.Models;
using Oybab.ServerManager.Model.Service.Order;
using Oybab.ServerManager.Model.Service.Takeout;
using Oybab.ServerManager.Model.Service.Product;
using Oybab.ServerManager.Model.Service.Device;
using Oybab.ServerManager.Model.Service.OrderDetail;
using Oybab.ServerManager.Model.Service.TakeoutDetail;

namespace Oybab.TradingSystemX.Server
{
    internal sealed class NotificationService
    {

        #region Instance
        private NotificationService()
        {
        }

        private static readonly Lazy<NotificationService> _instance = new Lazy<NotificationService>(() => new NotificationService());
        public static NotificationService Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        internal object NotificationLock = new object();

        #endregion Instance

        /// <summary>
        /// 服务获取通知
        /// </summary>
        /// <param name="toClient"></param>
        internal void ServiceSendNotification(ToClientServiceSendNotification toClient)
        {
            try
            {
                if (toClient.SendType == SendType.Call)
                {
                    if (!Resources.Instance.CallNotifications.ContainsKey(toClient.RoomId.Value))
                    {
                        Resources.Instance.CallNotifications.Add(toClient.RoomId.Value, true);
                    }
                    else
                    {
                        Resources.Instance.CallNotifications[toClient.RoomId.Value] = true;
                    }

                    Notification.Instance.ActionSendFromService(null, toClient.RoomId.Value, "Call");
                }
                else if (toClient.SendType == SendType.Online || toClient.SendType == SendType.Offline)
                {
                    RoomModel model = Resources.Instance.RoomsModel.Where(x => x.RoomId == toClient.RoomId).FirstOrDefault();

                    if (toClient.SendType == SendType.Online)
                        model.State = 1;
                    else if (toClient.SendType == SendType.Offline)
                        model.State = 0;

                    Notification.Instance.ActionSendFromService(null, toClient.RoomId.Value, null);
                }
                else if (toClient.SendType == SendType.FireOn || toClient.SendType == SendType.FireOff)
                {
                    Resources.Instance.IsFireAlarmEnable = (toClient.SendType == SendType.FireOn ? true : false);
                    Notification.Instance.ActionSend(null, -1, null);
                }
                else if (toClient.SendType == SendType.ExtendInfo)
                {
                    Resources.Instance.ExtendInfo = JsonConvert.DeserializeObject<ExtendInfo>(toClient.Model);
                    Notification.Instance.ActionSend(null, -2, null);
                }
            }
            catch (Exception ex)
            {
                ExceptionPro.ExpLog(ex);
            }
        }

        /// <summary>
        /// 订单更新
        /// </summary>
        /// <param name="toClient"></param>
        internal void ServiceOrderUpdateNotification(ToClientServiceOrderUpdateNotification toClient)
        {
            try
            {

                List<OrderNotificationModel> list = toClient.OrderNotification.DeserializeObject<List<OrderNotificationModel>>();

                List<long> Rooms = new List<long>();
                foreach (var item in list)
                {
                    // 获取订单模型
                    RoomModel model = Resources.Instance.RoomsModel.Where(x => x.RoomId == item.RoomId).FirstOrDefault();
                    // 如果模型不存在
                    if (null == model)
                    {
                        // 获取本地雅座
                        Room room = Resources.Instance.Rooms.Where(x => x.RoomId == item.RoomId).FirstOrDefault();
                        // 如果本地雅座存在, 并且这不是新增则新增模型(删除,或者 编辑,新增隐藏的包厢时)
                        if (null != room && null != item.OrderSessionId)
                        {
                            model = new RoomModel() { RoomId = room.RoomId, Order = room.Order, RoomNo = room.RoomNo, HideType = room.HideType, OrderSession = item.OrderSessionId };
                            Resources.Instance.RoomsModel.Add(model);
                        }
                    }
                    // 如果本地模型存在
                    else if (null != model)
                    {
                        // 如果远程是删除状态, 并且如果不是新增则新增模型(删除,或者 编辑,新增隐藏的包厢时)
                        if (null == item.OrderSessionId)
                            Resources.Instance.RoomsModel.Remove(model);
                        else
                        {
                            // 将新改动信息覆盖
                            model.PayOrder = item.Order;
                            model.OrderSession = item.OrderSessionId;

                        }
                    }

                    Rooms.Add(model.RoomId);



                    // 如果有产品则更新产品
                    if (null != item.ProductsChange)
                    {
                        foreach (var product in item.ProductsChange)
                        {
                            Product currentProduct = Resources.Instance.Products.Where(x => x.ProductId == product.Key).FirstOrDefault();
                            currentProduct.UpdateTime = product.Value.Product.UpdateTime;
                            currentProduct.BalanceCount = Math.Round(currentProduct.BalanceCount + product.Value.CountChange, 3);
                            Notification.Instance.ActionProduct(null, product.Value.Product, null);
                        }
                    }
                }

                Notification.Instance.ActionSendsFromService(null, Rooms, null);
            }
            catch (Exception ex)
            {
                TradingSystemX.Server.QueueMessageBoxNotification.Instance.ActionMessageBox(null, null, Tools.CommandTitles.Instance.Information, "ServiceOrderUpdateNotificationError: " + ex.Message + ex.StackTrace, VM.ViewModels.Controls.MessageBoxMode.Dialog, VM.ViewModels.Controls.MessageBoxImageMode.Information, VM.ViewModels.Controls.MessageBoxButtonMode.OK, null, null);
                ExceptionPro.ExpLog(ex);
            }
        }

        /// <summary>
        /// 外卖更新
        /// </summary>
        /// <param name="toClient"></param>
        internal void ServiceTakeoutUpdateNotification(ToClientServiceTakeoutUpdateNotification toClient)
        {
            try
            {
                List<TakeoutNotificationModel> list = toClient.TakeoutNotification.DeserializeObject<List<TakeoutNotificationModel>>();

                foreach (var item in list)
                {

                    // 如果有产品则更新产品
                    if (null != item.ProductsChange)
                    {
                        foreach (var product in item.ProductsChange)
                        {
                            Product currentProduct = Resources.Instance.Products.Where(x => x.ProductId == product.Key).FirstOrDefault();
                            currentProduct.UpdateTime = product.Value.Product.UpdateTime;
                            currentProduct.BalanceCount = Math.Round(currentProduct.BalanceCount + product.Value.CountChange, 3);
                            Notification.Instance.ActionProduct(null, product.Value.Product, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionPro.ExpLog(ex);
            }
        }





        /// <summary>
        /// 产品数量更新
        /// </summary>
        /// <param name="toClient"></param>
        internal void ServiceProductCountUpdateNotification(ToClientServiceProductCountUpdateNotification toClient)
        {
            try
            {

                Dictionary<long, ProductWithCount> list = toClient.ProductAndCounts.DeserializeObject<Dictionary<long, ProductWithCount>>();
                // 如果有产品则更新产品
                if (null != list)
                {
                    foreach (var product in list.Values)
                    {
                        Product currentProduct = Resources.Instance.Products.Where(x => x.ProductId == product.Product.ProductId).FirstOrDefault();
                        currentProduct.UpdateTime = product.Product.UpdateTime;
                        currentProduct.BalanceCount = Math.Round(currentProduct.BalanceCount + product.CountChange, 3);

                        // 如果支出价格有变动则修改
                        if (null != product.NewCostPrice)
                            currentProduct.CostPrice = product.NewCostPrice.Value;
                        // 如果价格有变动,则修改
                        if (null != product.NewPrice)
                            currentProduct.Price = product.NewPrice.Value;

                        Notification.Instance.ActionProduct(null, product.Product, null);
                    }


                }
            }
            catch (Exception ex)
            {

                ExceptionPro.ExpLog(ex);
            }
        }

        /// <summary>
        /// 设备登录或退出等状态有变
        /// </summary>
        /// <param name="toClient"></param>
        internal void ServiceDeviceModeUpdateNotification(ToClientServiceDeviceModeUpdateNotification toClient)
        {
            throw new NotImplementedException();
        }




        /// <summary>
        /// 服务订单新增明细通知(客户端顾客先验证模式)
        /// </summary>
        /// <param name="toClient"></param>
        internal void ServiceOrderDetailsAddNotification(ToClientServiceOrderDetailsAddNotification toClient)
        {
            try
            {

                Order order = toClient.Order.DeserializeObject<Order>();

                RoomModel model = Resources.Instance.RoomsModel.Where(x => x.RoomId == order.RoomId).FirstOrDefault();

                if (null != model)
                {
                    model.PayOrder = order;
                }


                Notification.Instance.ActionSendFromService(null, model.RoomId, null);
            }
            catch (Exception ex)
            {

                ExceptionPro.ExpLog(ex);
            }
        }



        /// <summary>
        /// 服务外卖新增外卖通知(客户端顾客先验证模式)
        /// </summary>
        /// <param name="toClient"></param>
        internal void ServiceTakeoutAddNotification(ToClientServiceTakeoutAddNotification toClient)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// 服务发送模型更改通知
        /// </summary>
        /// <param name="toClient"></param>
        internal void ServiceModelUpdateNotification(ToClientServiceModelUpdateNotification toClient)
        {
            try
            {
                switch (toClient.ModelType)
                {
                    case ModelType.Product:

                        Product product = toClient.Model.DeserializeObject<Product>();
                        Product oldProduct = Resources.Instance.Products.Where(x => x.ProductId == product.ProductId).FirstOrDefault();
                        switch (toClient.OperateType)
                        {
                            case OperateType.Add:
                                Resources.Instance.Products.Add(product);
                                break;
                            case OperateType.Edit:
                                Resources.Instance.Products.Remove(oldProduct);
                                Resources.Instance.Products.Add(product);
                                break;
                            case OperateType.Delete:
                                Resources.Instance.Products.Remove(oldProduct);
                                break;
                        }
                        Notification.Instance.ActionProduct(null, product, (int)toClient.OperateType);
                        break;
                    case ModelType.ProductType:
                        ProductType productType = toClient.Model.DeserializeObject<ProductType>();
                        ProductType oldProductType = Resources.Instance.ProductTypes.Where(x => x.ProductTypeId == productType.ProductTypeId).FirstOrDefault();
                        switch (toClient.OperateType)
                        {
                            case OperateType.Add:
                                Resources.Instance.ProductTypes.Add(productType);
                                break;
                            case OperateType.Edit:
                                Resources.Instance.ProductTypes.Remove(oldProductType);
                                Resources.Instance.ProductTypes.Add(productType);
                                break;
                            case OperateType.Delete:
                                Resources.Instance.ProductTypes.Remove(oldProductType);
                                break;
                        }
                        Notification.Instance.ActionProductType(null, productType, (int)toClient.OperateType);
                        break;
                    case ModelType.Admin:
                        Admin admin = toClient.Model.DeserializeObject<Admin>();
                        Admin oldAdmin = Resources.Instance.Admins.Where(x => x.AdminId == admin.AdminId).FirstOrDefault();
                        switch (toClient.OperateType)
                        {
                            case OperateType.Add:
                                Resources.Instance.Admins.Add(admin);
                                break;
                            case OperateType.Edit:
                                Resources.Instance.Admins.Remove(oldAdmin);
                                Resources.Instance.Admins.Add(admin);

                                if (Resources.Instance.AdminModel.AdminId == admin.AdminId)
                                    Resources.Instance.AdminModel = admin;
                                break;
                            case OperateType.Delete:
                                Resources.Instance.Admins.Remove(oldAdmin);
                                break;
                        }
                        Notification.Instance.ActionAdmin(null, admin, (int)toClient.OperateType);
                        break;
                    case ModelType.Device:
                        Device device = toClient.Model.DeserializeObject<Device>();
                        Device oldDevice = Resources.Instance.Devices.Where(x => x.DeviceId == device.DeviceId).FirstOrDefault();
                        switch (toClient.OperateType)
                        {
                            case OperateType.Add:
                                Resources.Instance.Devices.Add(device);
                                break;
                            case OperateType.Edit:
                                Resources.Instance.Devices.Remove(oldDevice);
                                Resources.Instance.Devices.Add(device);
                                break;
                            case OperateType.Delete:
                                Resources.Instance.Devices.Remove(oldDevice);
                                break;
                        }
                        Notification.Instance.ActionDevice(null, device, (int)toClient.OperateType);
                        break;
                    case ModelType.Printer:
                        Printer printer = toClient.Model.DeserializeObject<Printer>();
                        Printer oldPrinter = Resources.Instance.Printers.Where(x => x.PrinterId == printer.PrinterId).FirstOrDefault();
                        switch (toClient.OperateType)
                        {
                            case OperateType.Add:
                                Resources.Instance.Printers.Add(printer);
                                break;
                            case OperateType.Edit:
                                Resources.Instance.Printers.Remove(oldPrinter);
                                Resources.Instance.Printers.Add(printer);
                                break;
                            case OperateType.Delete:
                                Resources.Instance.Printers.Remove(oldPrinter);
                                break;
                        }
                        Notification.Instance.ActionPrinter(null, printer, (int)toClient.OperateType);
                        break;
                    case ModelType.Request:
                        Request request = toClient.Model.DeserializeObject<Request>();
                        Request oldRequest = Resources.Instance.Requests.Where(x => x.RequestId == request.RequestId).FirstOrDefault();
                        switch (toClient.OperateType)
                        {
                            case OperateType.Add:
                                Resources.Instance.Requests.Add(request);
                                break;
                            case OperateType.Edit:
                                Resources.Instance.Requests.Remove(oldRequest);
                                Resources.Instance.Requests.Add(request);
                                break;
                            case OperateType.Delete:
                                Resources.Instance.Requests.Remove(oldRequest);
                                break;
                        }
                        Notification.Instance.ActionRequest(null, request, (int)toClient.OperateType);
                        break;
                    case ModelType.Ppr:
                        List<Ppr> pprs = toClient.Model.DeserializeObject<List<Ppr>>();
                        Product theProduct = toClient.ModelRef.DeserializeObject<Product>();
                        switch (toClient.OperateType)
                        {
                            case OperateType.Add:
                                Resources.Instance.Pprs.AddRange(pprs);
                                break;
                            case OperateType.Edit:
                                foreach (var item in Resources.Instance.Pprs.Where(x => x.ProductId == theProduct.ProductId).ToList())
                                {
                                    Resources.Instance.Pprs.Remove(item);
                                }
                                if (pprs.Count > 0)
                                    Resources.Instance.Pprs.AddRange(pprs);

                                break;
                            case OperateType.Delete:
                                foreach (var item in Resources.Instance.Pprs.Where(x => x.ProductId == theProduct.ProductId).ToList())
                                {
                                    Resources.Instance.Pprs.Remove(item);
                                }
                                break;
                        }
                        Notification.Instance.ActionPprs(null, pprs, theProduct, (int)toClient.OperateType);
                        break;
                    case ModelType.Member:
                        Member member = toClient.Model.DeserializeObject<Member>();
                        switch (toClient.OperateType)
                        {
                            case OperateType.Add:
                                break;
                            case OperateType.Edit:
                                break;
                            case OperateType.Delete:
                                break;
                        }
                        Notification.Instance.ActionMember(null, member, (int)toClient.OperateType);
                        break;
                    case ModelType.Balance:
                        Balance balance = toClient.Model.DeserializeObject<Balance>();
                        Balance oldBalance = Resources.Instance.Balances.Where(x => x.BalanceId == balance.BalanceId).FirstOrDefault();
                        switch (toClient.OperateType)
                        {
                            case OperateType.Add:
                                Resources.Instance.Balances.Add(balance);
                                break;
                            case OperateType.Edit:
                                Resources.Instance.Balances.Remove(oldBalance);
                                Resources.Instance.Balances.Add(balance);
                                break;
                            case OperateType.Delete:
                                Resources.Instance.Balances.Remove(oldBalance);
                                break;
                        }
                        Notification.Instance.ActionBalance(null, balance, (int)toClient.OperateType);
                        break;
                    case ModelType.Room:
                        Room room = toClient.Model.DeserializeObject<Room>();
                        Room oldRoom = Resources.Instance.Rooms.Where(x => x.RoomId == room.RoomId).FirstOrDefault();

                        RoomModel oldRoomModel = Resources.Instance.RoomsModel.Where(x => x.RoomId == room.RoomId).FirstOrDefault();
                        RoomModel newRoomModel = toClient.ModelRef.DeserializeObject<RoomModel>();
                        switch (toClient.OperateType)
                        {
                            case OperateType.Add:
                                Resources.Instance.Rooms.Add(room);

                                if (null != newRoomModel && newRoomModel.RoomId > 0)
                                {
                                    Resources.Instance.RoomsModel.Add(newRoomModel);
                                }
                                break;
                            case OperateType.Edit:
                                Resources.Instance.Rooms.Remove(oldRoom);
                                Resources.Instance.Rooms.Add(room);


                                if (null != oldRoomModel && oldRoomModel.RoomId > 0)
                                {
                                    Resources.Instance.RoomsModel.Remove(oldRoomModel);
                                }
                                if (null != newRoomModel && newRoomModel.RoomId > 0 && newRoomModel.HideType != 1)
                                {
                                    Resources.Instance.RoomsModel.Add(newRoomModel);
                                }


                                break;
                            case OperateType.Delete:
                                Resources.Instance.Rooms.Remove(oldRoom);

                                if (null != oldRoomModel && oldRoomModel.RoomId > 0)
                                {
                                    Resources.Instance.RoomsModel.Remove(oldRoomModel);
                                    Notification.Instance.ActionSendFromService(null, oldRoomModel.RoomId, null);
                                }

                                break;
                        }
                        Notification.Instance.ActionRoom(null, room, (int)toClient.OperateType);

                        if (null != newRoomModel && newRoomModel.RoomId > 0)
                            Notification.Instance.ActionSendFromService(null, newRoomModel.RoomId, null);

                        break;
                    case ModelType.Config:
                        switch (toClient.OperateType)
                        {
                            case OperateType.Edit:
                                List<string> config = toClient.Model.DeserializeObject<List<string>>();
                                foreach (var item in config)
                                {
                                    if (item.Contains("PrintInfo="))
                                    {
                                        string temp = item.Trim().TrimStart("PrintInfo=");
                                        Resources.Instance.PrintInfo = JsonConvert.DeserializeObject<PrintInfo>(temp);
                                    }
                                }
                                break;
                        }
                        Notification.Instance.ActionConfig(null, null, (int)toClient.OperateType);
                        break;
                    case ModelType.Import:
                        Import import = toClient.Model.DeserializeObject<Import>();
                        switch (toClient.OperateType)
                        {
                            case OperateType.Add:
                                break;
                            case OperateType.Edit:
                                break;
                            case OperateType.Delete:
                                break;
                        }
                        Notification.Instance.ActionImport(null, import, (int)toClient.OperateType);
                        break;
                    case ModelType.AdminLog:
                        AdminLog adminLog = toClient.Model.DeserializeObject<AdminLog>();
                        switch (toClient.OperateType)
                        {
                            case OperateType.Add:
                                break;
                            case OperateType.Edit:
                                break;
                            case OperateType.Delete:
                                break;
                        }
                        Notification.Instance.ActionAdminLog(null, adminLog, (int)toClient.OperateType);
                        break;

                    case ModelType.Supplier:
                        Supplier supplier = toClient.Model.DeserializeObject<Supplier>();
                        switch (toClient.OperateType)
                        {
                            case OperateType.Add:
                                break;
                            case OperateType.Edit:
                                break;
                            case OperateType.Delete:
                                break;
                        }
                        Notification.Instance.ActionSupplier(null, supplier, (int)toClient.OperateType);
                        break;
                    // 各类支付暂时不用
                    case ModelType.SupplierPay:

                        break;
                    case ModelType.AdminPay:

                        break;
                    case ModelType.MemberPay:

                        break;
                    case ModelType.BalancePay:

                        break;
                    case ModelType.CallBack:
                        List<NotificationCache> cache = toClient.Model.DeserializeObject<List<NotificationCache>>();
                        switch (toClient.OperateType)
                        {
                            case OperateType.Get:
                                foreach (var item in cache)
                                {
                                    switch (item.Type)
                                    {
                                        case NotificationType.Send:
                                            NotificationService.Instance.ServiceSendNotification(item.Notification.DeserializeObject<ToClientServiceSendNotification>());
                                            break;
                                        case NotificationType.ProductCountUpdate:
                                            NotificationService.Instance.ServiceProductCountUpdateNotification(item.Notification.DeserializeObject<ToClientServiceProductCountUpdateNotification>());
                                            break;
                                        case NotificationType.OrderUpdate:
                                            NotificationService.Instance.ServiceOrderUpdateNotification(item.Notification.DeserializeObject<ToClientServiceOrderUpdateNotification>());
                                            break;
                                        case NotificationType.OrderDetailsAdd:
                                            NotificationService.Instance.ServiceOrderDetailsAddNotification(item.Notification.DeserializeObject<ToClientServiceOrderDetailsAddNotification>());
                                            break;
                                        case NotificationType.TakeoutUpdate:
                                            NotificationService.Instance.ServiceTakeoutUpdateNotification(item.Notification.DeserializeObject<ToClientServiceTakeoutUpdateNotification>());
                                            break;
                                        case NotificationType.ModelUpdate:
                                            NotificationService.Instance.ServiceModelUpdateNotification(item.Notification.DeserializeObject<ToClientServiceModelUpdateNotification>());
                                            break;
                                    }
                                }
                                break;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                TradingSystemX.Server.QueueMessageBoxNotification.Instance.ActionMessageBox(null, null, Tools.CommandTitles.Instance.Information, "ServiceModelUpdateNotificationError: " + ex.Message + ex.StackTrace, VM.ViewModels.Controls.MessageBoxMode.Dialog, VM.ViewModels.Controls.MessageBoxImageMode.Information, VM.ViewModels.Controls.MessageBoxButtonMode.OK, null, null);
                ExceptionPro.ExpLog(ex);
            }
        }
    }
}
