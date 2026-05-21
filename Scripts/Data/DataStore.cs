using System;
using System.Text.Json;
using Godot;

namespace MineSweeper;

public static class DataStore
{
    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true,
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
    };

    public static T Load<T>(string relativePath) where T : new() {
        try {
            string fullPath = ProjectSettings.GlobalizePath($"user://{relativePath}");
            if (!FileAccess.FileExists(fullPath)) {
                return new T();
            }

            using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Read);
            string json = file.GetAsText();
            return JsonSerializer.Deserialize<T>(json, Options) ?? new T();
        } catch (Exception e) {
            GD.PushError($"Failed to load {relativePath}: {e.Message}");
            return new T();
        }
    }

    public static T LoadRes<T>(string resPath) where T : new() {
        try {
            if (!FileAccess.FileExists(resPath)) {
                GD.PushError($"Config file not found: {resPath}");
                return new T();
            }

            using var file = FileAccess.Open(resPath, FileAccess.ModeFlags.Read);
            string json = file.GetAsText();
            return JsonSerializer.Deserialize<T>(json, Options) ?? new T();
        } catch (Exception e) {
            GD.PushError($"Failed to load config {resPath}: {e.Message}");
            return new T();
        }
    }

    public static void Save<T>(string relativePath, T data) {
        try {
            string fullPath = ProjectSettings.GlobalizePath($"user://{relativePath}");
            string dir = fullPath[..fullPath.LastIndexOf('/')];
            if (!DirAccess.DirExistsAbsolute(dir)) {
                DirAccess.MakeDirRecursiveAbsolute(dir);
            }

            using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Write);
            string json = JsonSerializer.Serialize(data, Options);
            file.StoreString(json);
        } catch (Exception e) {
            GD.PushError($"Failed to save {relativePath}: {e.Message}");
        }
    }
}