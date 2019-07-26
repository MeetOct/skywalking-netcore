using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SkyWalking.Sample.Frontend
{
    /// <summary>
    /// 笔记本检测软件版本更新信息
    /// </summary>
    [Table("notebookinspection_app_release_note")]
    public class AppReleaseNote : BaseEntity
    {
        public AppReleaseNote()
        {
        }
        public AppReleaseNote(int id)
        {
            Id = id;
        }
        /// <summary>
        /// 版本号
        /// </summary>
        [Column("version_number")]
        [Required(AllowEmptyStrings = false)]
        [Range(1, 10)]
        public string VersionNumber { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        [Column("description")]
        [MaxLength(256)]
        public string Description { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("create_dt")]
        [Required]
        public DateTime CreateDt { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        [Column("create_by")]
        [Required]
        public int CreateBy { get; set; }
    }
}
