using System;
using System.IO;
using DesignDataTable;
using DesignDataTable.Editor;
using UnityEditor;
using UnityEngine;

namespace DotAction.Editor
{
    /// <summary>
    /// DDT의 공개 생성 API로 전투 원본과 바이너리를 최초 생성한다.
    /// </summary>
    public static class CombatDataSetup
    {
        public static void Create()
        {
            string path = "Assets/Data/CombatStats.xlsx";
            if (File.Exists(path))
            {
                throw new InvalidOperationException("기존 전투 데이터는 DDT에서 편집하세요.");
            }
            var table = new TableInfo { name = "CombatStats", sourceSheet = "CombatStats" };
            table.columns.Add(new Column(eColumnType.Int, "Id") { isUniqueKey = true });
            table.columns.Add(new Column(eColumnType.String, "Label"));
            foreach (string name in new[] { "Health", "Speed", "Damage", "Range", "Cooldown" })
            {
                table.columns.Add(new Column(eColumnType.Float, name));
            }
            string[][] values =
            {
                new[] { "1", "Ranger", "120", "4.5", "20", "7", "0.65" },
                new[] { "2", "Goblin", "40", "1.65", "8", "1.15", "1.1" },
                new[] { "3", "Skeleton", "60", "1.25", "12", "1.25", "1.4" }
            };
            foreach (string[] valuesRow in values)
            {
                var row = new Row();
                foreach (string value in valuesRow)
                {
                    row.cells.Add(new Cell { value = value });
                }
                table.rows.Add(row);
            }
            table.UpdateCellName();
            TableCreator.CreateDesignData("CombatStats", path, table);
            TableCreator.RefreshGenerated(table);
            AssetDatabase.SaveAssets();
            Debug.Log("DOTACTION_DDT_CREATED: " + path + "; rows=3");
        }
    }
}
