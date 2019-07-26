using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkyWalking.Sample.Frontend
{
    /// <summary>
    /// ErpDB上下文
    /// </summary>
    public class ErpDbContext : DbContext
    {

        /// <summary>
        /// 笔记本检测软件版本更新信息
        /// </summary>
        public DbSet<AppReleaseNote> NotebookAppReleaseNotes { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options"></param>
        public ErpDbContext(DbContextOptions<ErpDbContext> options) : base(options)
        {

        }

        ///// <summary>
        ///// 创建model时触发的事件
        ///// </summary>
        ///// <param name="modelBuilder"></param>
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.Entity<AppInspectionRelayBoxConfig>(t =>
        //    {
        //        t.HasIndex(a => a.CategoryId);
        //    });

        //    modelBuilder.Entity<GraphicsMappingOptLog>(t => t.HasIndex(a => a.MappingId));

        //    modelBuilder.Entity<AppVersion>()
        //        .Property(b => b.CreatedDt)
        //        .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        //    modelBuilder.Entity<PrivateEraseProduct>(t => t.HasIndex(a => a.ProductNo));
        //    modelBuilder.Entity<PrivateEraseProduct>(t => t.HasIndex(a => a.Imei));

        //    modelBuilder.Entity<PrivateEraseReport>(t => t.HasIndex(a => a.Imei));
        //    //获取需要上传隐私清除报告的地方使用
        //    modelBuilder.Entity<PrivateEraseReport>(t => t.HasIndex(a => a.ReportUrl));
        //    modelBuilder.Entity<PrivateEraseReport>(t => t.HasIndex(a => a.Uuid));
        //    modelBuilder.Entity<PrivateEraseReport>(t => t.HasIndex(a => a.OrderNo));
        //    modelBuilder.Entity<PrivateEraseReport>(t => t.HasIndex(a => a.EraseStartTime));
        //    modelBuilder.Entity<PrivateEraseReport>(t => t.HasIndex(a => a.CategoryId));
        //    modelBuilder.Entity<PrivateEraseReport>(t => t.HasIndex(a => a.OperationalCentreId));
        //    modelBuilder.Entity<PrivateEraseReport>(t => t.HasIndex(a => a.ProductStatus));
        //    modelBuilder.Entity<PrivateEraseReport>(t => t.HasIndex(a => a.Status));
        //    modelBuilder.Entity<PrivateEraseReport>()
        //        .Property(b => b.CreatedDt)
        //       .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        //    modelBuilder.Entity<PrivateEraseReport>()
        //        .Property(b => b.Status)
        //       .HasDefaultValue((int)PrivateEraseReportStatus.未清除);
        //    modelBuilder.Entity<PrivateEraseReport>()
        //        .Property(b => b.ProductStatus)
        //       .HasDefaultValue((int)PrivateEraseProductStatus.待清除);
        //    modelBuilder.Entity<PrivateEraseReport>()
        //        .Property(b => b.EraseType)
        //        .HasDefaultValue(0);

        //    modelBuilder.Entity<PrivateEraseReportPolling>(t => t.HasIndex(a => a.Imei));
        //    modelBuilder.Entity<PrivateEraseReportPolling>(t => t.HasIndex(a => a.FinishPoolingDt));

        //    modelBuilder.Entity<PrivateEraseReportPolling>()
        //        .Property(b => b.CreateDt)
        //       .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        //    modelBuilder.Entity<AppUploadAttachment>()
        //        .Property(b => b.CreateDt)
        //       .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
        //    modelBuilder.Entity<AppUploadAttachment>(t => t.HasIndex(a => a.ReferSerialNo));

        //}
    }
}
