using System;
using System.Text;

namespace Quick.EntityFrameworkCore.Plus.Utils;

internal class ExceptionUtils
{
    public static string GetExceptionString(Exception ex)
    {
        StringBuilder sb = new StringBuilder();
        Exception tmpEx = ex;
        while (tmpEx != null)
        {
            sb.AppendLine("------------------------------------------------------");
            sb.AppendLine("异常类型：" + tmpEx.GetType().FullName);
            sb.AppendLine("异常消息：" + tmpEx.Message);
            sb.AppendLine("异常堆栈：" + tmpEx.StackTrace);
            if (tmpEx.InnerException != null && tmpEx.InnerException == tmpEx)
                break;
            tmpEx = tmpEx.InnerException;
        }
        return sb.ToString();
    }

    public static string GetExceptionMessage(Exception ex)
    {
        StringBuilder sb = new StringBuilder();
        Exception tmpEx = ex;
        while (tmpEx != null)
        {
            sb.Append(">");
            sb.AppendLine(tmpEx.Message);
            if (tmpEx.InnerException != null && tmpEx.InnerException == tmpEx)
                break;
            tmpEx = tmpEx.InnerException;
        }
        return sb.ToString();
    }
}
