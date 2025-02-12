using FluentMigrator;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Data.Migrations;

namespace Nop.Web.Framework.Migrations.UpgradeTo490;

[NopMigration("2025-02-12 00:00:00", "Pseudo-migration to update appSettings.json file", MigrationProcessType.Update)]
public class AppSettingsMigration : MigrationBase
{
    protected readonly AppSettings _appSettings;
    protected readonly INopFileProvider _fileProvider;

    public AppSettingsMigration(AppSettings appSettings,
        INopFileProvider fileProvider)
    {
        _appSettings = appSettings;
        _fileProvider = fileProvider;
    }

    public override void Up()
    {
        //#7569
        var commonConfig = _appSettings.Get<CommonConfig>();

        AppSettingsHelper.SaveAppSettings(new List<IConfig> { commonConfig }, _fileProvider);
    }

    public override void Down() { }
}