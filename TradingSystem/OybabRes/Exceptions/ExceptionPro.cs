﻿using log4net;
using Oybab.Res.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oybab.Res.Exceptions
{
    /// <summary>
    /// 异常处理器
    /// </summary>
    public static class ExceptionPro
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(ExceptionPro));
        public static Action Sync = null;
        /// <summary>
        /// 检测异常类型并对应操作
        /// </summary>
        /// <param name="ex"></param>
        public static string HandleExceptionType(Exception ex, string message = null, string ExceptionMessage = null)
        {
            if (null == ex)
                return "Not caption exception param!";

            Type type = ex.GetType();
            string tempStr = "";
            //找到异常

            switch (type.Name)
            {
                case "FormatException":
                    if (null != ex.Message && (ex.Message == "输入字符串的格式不正确。" || ex.Message == "Input string was not in a correct format."))
                        tempStr = string.Format(Resources.GetRes().GetString("InputNotValid"));
                    break;
                case "ArgumentException": // 下面的汉语部分是我自己翻译的, 也许不对导致出问题
                    if (null != ex.Message && (ex.Message == "输入对象必须是字符串。" || ex.Message == "Object must be of type String."))
                        tempStr = string.Format(Resources.GetRes().GetString("InputNotValid"));
                    break;
                case "MessageSecurityException":
                    if (null != ex.Message && (ex.Message.StartsWith("The security timestamp is invalid because its creation time") || (null != ex.InnerException && ex.InnerException.Message.StartsWith("验证消息的安全性时发生错误。"))))
                    {
                        tempStr = string.Format(Resources.GetRes().GetString("SystemTimeInvalid"), Common.GetCommon().GetFormat());
                        // 重新尝试同步一下时间
                        if (null != Sync)
                        {
                            Sync();
                        }
                    }
                    break;
                case "InvalidOperationException":
                    if (null != ex.Message && (ex.Message == "Operation did not succeed because the program cannot commit or quit a cell value change."))
                        tempStr = string.Format(Resources.GetRes().GetString("InputNotValid"));
                    break;
                
                default:
                    break;
            }
            if (null != ex as OybabException)
            {
                return (ex as OybabException).ExceptionMessage;
            }
            if (null != message)
            {
                if (!string.IsNullOrWhiteSpace(tempStr))
                    return tempStr;
                else 
                    return message;
            }
            else
            {
                //如果没找到异常,直接获取异常自己的信息
                if (tempStr == "")
                {
                    StringBuilder tempMessage = new StringBuilder();
                    GetExceptionAllMessage(ex, tempMessage);
                    return tempMessage.ToString();
                }
                else
                {
                    return tempStr;
                }
            }
        }

        /// <summary>
        /// 获取所有异常
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="Message"></param>
        public static void GetExceptionAllMessage(Exception ex, StringBuilder Message)
        {
            Message.AppendLine(ex.Message);
            if (null != ex.InnerException)
            {
                GetExceptionAllMessage(ex.InnerException, Message);
            }
        }

        /// <summary>
        /// 输出异常
        /// </summary>
        /// <param name="message"></param>
        public static void ExpErrorLog(string message)
        {
            try
            {
                logger.Error(message);
            }
            catch { }
            
        }

        /// <summary>
        /// 输出信息
        /// </summary>
        /// <param name="message"></param>
        public static void ExpInfoLog(string message)
        {
            try
            {
                logger.Info(message);
            }
            catch { }
        }

        /// <summary>
        /// 异常处理Exception
        /// </summary>
        /// <param name="ex"></param>
        public static void ExpLog(Exception ex, Action<string> action = null, bool IsInfo = false, string designationMessage = "")
        {
            OybabException vodEx = ex as OybabException;
            if (null != vodEx)
            {
                if (null != action)
                    AlertMessage(action, vodEx.ExceptionMessage, designationMessage);
            }
            else
            {
                string message = HandleExceptionType(ex);
                if (IsInfo)
                    ExpInfoLog(message + designationMessage + ex.ToString());
                else
                    ExpErrorLog(message + designationMessage + ex.ToString());
                if (null != action)
                    AlertMessage(action, message, designationMessage);
            }
        }


        /// <summary>
        /// 异常处理Exception
        /// </summary>
        /// <param name="ex"></param>
        public static void ExpLog(object exception, Action<string> action = null, bool IsInfo = false, string designationMessage = "")
        {

            Exception ex = exception as Exception;

            if (null != ex)
            {
                ExpLog(ex, action, IsInfo, designationMessage);
            }
            else
            {
                if (IsInfo)
                    ExpInfoLog(exception.ToString() + designationMessage);
                else
                    ExpErrorLog(exception.ToString() + designationMessage);
                if (null != action)
                    AlertMessage(action, exception.ToString(), designationMessage);
            }

        }

        /// <summary>
        /// 提醒用户
        /// </summary>
        /// <param name="action"></param>
        /// <param name="message"></param>
        /// <param name="designationMessage"></param>
        private static void AlertMessage(Action<string> action, string message, string designationMessage)
        {
            if (string.IsNullOrWhiteSpace(message))
                action(designationMessage);
            else
                action(message);
        }

    }
}
