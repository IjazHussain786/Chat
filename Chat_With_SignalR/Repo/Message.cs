//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Repo
{
    using System;
    using System.Collections.Generic;
    
    public partial class Message
    {
        public int MessageID { get; set; }
        public int UserID { get; set; }
        public int RoomID { get; set; }
        public string MessageText { get; set; }
    
        public virtual Room Room { get; set; }
        public virtual User User { get; set; }
    }
}
