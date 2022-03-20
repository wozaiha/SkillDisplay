﻿using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace SkillDisplay;

internal class PluginUI : IDisposable
{
    private Configuration config;

    public bool SettingsVisible = false;
    private Plugin _plugin;

    private ExcelSheet<Action> Action = DalamudApi.DataManager.GetExcelSheet<Action>();

    public class Skill
    {
        public Action Action;
        public long Time;
        public ActionType Type;

        public enum ActionType
        {
            Cast,
            Do,
            Cancel
        }

        public Skill(Action action, long time, ActionType type)
        {
            Action = action;
            Time = time;
            Type = type;
        }
    }

    private List<Skill> Skilllist = new();
    public static Dictionary<uint, TextureWrap?> Icon = new();
    private ImDrawListPtr window;

    public PluginUI(Plugin p)
    {
        _plugin = p;
        config = p.Configuration;
        DalamudApi.PluginInterface.UiBuilder.Draw += Draw;
        DalamudApi.PluginInterface.UiBuilder.Draw += DrawConfig;
        DalamudApi.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        if (!Icon.ContainsKey(0))
            Icon.TryAdd(0,
                DalamudApi.DataManager.GetImGuiTextureHqIcon(0));
    }

    public void Dispose()
    {
        DalamudApi.PluginInterface.UiBuilder.Draw -= Draw;
        DalamudApi.PluginInterface.UiBuilder.Draw -= DrawConfig;
        DalamudApi.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        foreach (var (id, icon) in Icon) icon.Dispose();
    }

    public void DoAction(uint actionId)
    {
        var action = Action.GetRow(actionId)!;
        var iconId = action.Icon;
        if (!Icon.ContainsKey(iconId))
            Icon.TryAdd(iconId,
                DalamudApi.DataManager.GetImGuiTextureHqIcon(iconId));
        Skilllist.Add(new Skill(action, DateTimeOffset.Now.ToUnixTimeMilliseconds(), Skill.ActionType.Do));
        PluginLog.Information($"Adding:{action.RowId}:{action.ActionCategory.Row}");
    }

    public void Cast(uint actionId)
    {
        var action = Action.GetRow(actionId)!;
        var iconId = action.Icon;
        if (!Icon.ContainsKey(iconId))
            Icon.TryAdd(iconId,
                DalamudApi.DataManager.GetImGuiTextureHqIcon(iconId));
        Skilllist.Add(new Skill(action, DateTimeOffset.Now.ToUnixTimeMilliseconds(), Skill.ActionType.Cast));
    }

    public void Cancel(uint actionId)
    {
        var action = Action.GetRow(actionId)!;
        var iconId = action.Icon;
        if (!Icon.ContainsKey(iconId))
            Icon.TryAdd(iconId,
                DalamudApi.DataManager.GetImGuiTextureHqIcon(iconId));
        Skilllist.Add(new Skill(action, DateTimeOffset.Now.ToUnixTimeMilliseconds(), Skill.ActionType.Cancel));
        PluginLog.Information($"Adding:{action.RowId}");
    }


    public void DrawConfigUI()
    {
        DrawConfig();
    }

    public void Draw()
    {
        ImGui.SetNextWindowSize(new Vector2(config.IconSize * 10, config.IconSize * 1.5f));
        ImGui.Begin("main", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize);

        window = ImGui.GetWindowDrawList();
        var color = ImGui.ColorConvertFloat4ToU32(config.color);

        for (var i = Skilllist.Count - 1; i >= 0; i--)
        {
            var size = new Vector2(config.IconSize);
            var speed = size.X / 600;
            var skill = Skilllist[i];
            var pos = ImGui.GetWindowPos() + ImGui.GetWindowSize() - size -
                      new Vector2((DateTimeOffset.Now.ToUnixTimeMilliseconds() - skill.Time) * speed - size.X/2f, size.Y / 2);
            if (skill.Action.ActionCategory.Row is 4) // 能力
            {
                pos = pos + new Vector2(0, size.Y / 2);
                size /= 1.5f;
            }

            if (skill.Action.ActionCategory.Row is 1) //自动攻击
            {
                pos = pos + new Vector2(0, size.Y);
                size /= 2;
            }

            if (skill.Type == Skill.ActionType.Cast)
            {
                size *= 0.6f;
                var target = ImGui.GetWindowPos() + new Vector2(ImGui.GetColumnWidth(), size.Y);
                for (var j = i + 1; j < Skilllist.Count; j++)
                {
                    if (Skilllist[j].Action.RowId != skill.Action.RowId) continue;
                    target = ImGui.GetWindowPos() + ImGui.GetWindowSize() - new Vector2(
                        (DateTimeOffset.Now.ToUnixTimeMilliseconds() - Skilllist[j].Time) * speed + config.IconSize/2,
                        config.IconSize * 1.5f - size.Y);
                    ImGui.Text($"{i}:{j}:{Skilllist.Count}");
                    break;
                }
                if (target.X - pos.X - size.X > 0) window.AddRectFilled(pos + new Vector2(size.X, 0), target, color);
            }

            //if ((i == 0) && (skill.Action.Cast100ms > 0) && (skill.Type == Skill.ActionType.Do))
            //{
            //    window.AddRectFilled(ImGui.GetWindowPos(),pos + size*0.6f, color);
            //}

            if (skill.Type != Skill.ActionType.Cancel) window.AddImage(Icon[skill.Action.Icon]!.ImGuiHandle, pos, pos + size);
            else window.AddImage(Icon[skill.Action.Icon]!.ImGuiHandle, pos, pos+Vector2.One);
            if ((DateTimeOffset.Now.ToUnixTimeMilliseconds() - skill.Time) * speed > ImGui.GetWindowWidth())
                Skilllist.Remove(skill);
        }
        ImGui.End();
    }

    void DrawConfig()
    {
        if (!SettingsVisible) return;
        ImGui.Begin("SkillConfig",ref SettingsVisible,ImGuiWindowFlags.AlwaysAutoResize);
        var size = (int)config.IconSize;
        var changed = false;
        ImGui.Text("Icon Size:");
        ImGui.SameLine();
        changed |= ImGui.InputInt("###Icon Size", ref size,1);
        changed |= ImGui.ColorPicker4("Connection Color", ref config.color,ImGuiColorEditFlags.NoInputs);
        if (changed)
        {
            config.IconSize = size;
            config.Save();
        }
        ImGui.End();
    }
}