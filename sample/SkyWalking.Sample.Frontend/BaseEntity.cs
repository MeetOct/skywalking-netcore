using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SkyWalking.Sample.Frontend
{
    public class BaseEntity
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Key, Column("id")]
        public int Id { get; set; }
    }
}
