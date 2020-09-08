//********************************************************************
// Filename: HostException.cs 
//___________________________________________________________________ 
// Application  : Host interface handling
// Main program : HostBridge.cs
// Version      : 1.0.0
// Status       : c         [[c]oding, [t]est, [e]rror, ok]
// Author       : Roberto Alaimo - Bit Automation
// Date         : 01.05.16
// ___________________________________________________________________
// Description  : Host Exceptions handling 
//                 
//_____________________________________________________________________
// Changes      :
// Date     Author      Description
//
//********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HostBridge
{
  public class HostException : System.Exception
  {
    public int ErrorCode
    { get; set; }

    public HostException(string message, int errorcode)
      : base(message)
    {
      this.ErrorCode = errorcode;
    }
  }
}
