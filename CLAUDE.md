# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 6 AR application for indoor lost item management. Users can photograph lost items for AI recognition and registration, then use AR navigation with a guide character to find them.

## Tech Stack

- **Unity 6** (productName: capstone_1)
- **Universal Render Pipeline (URP)** 17.2.0
- **AR Foundation** with XR Management 4.5.3
- **NavMesh AI Navigation** 2.0.9 for pathfinding
- **Pcx** (Keijiro's point cloud library) for PLY file rendering
- **Backend**: FastAPI server at `http://192.168.0.146:8000`

## Architecture

### UI Flow (3-panel system in UIManager)
1. **Main Panel** - Lost item list with category/color filtering (MainListController)
2. **Detail Panel** - Item details and location info (DetailViewController)
3. **Register Panel** - Camera capture → AI recognition → registration (RegisterViewController + WebCamController)

### Server Communication (ServerConnector)
- `POST /analyze` - Image upload for AI object recognition (returns label, colors, image_url)
- `POST /register` - Save item with position data (RegisterRequest DTO)
- `GET /objects` - Fetch all items (ObjectItem DTO with colors, description, anchor positions)

### AR Navigation System
- **NavigationManager**: Handles XR Origin positioning, NavMeshAgent spawning at user's camera position
- **GuideController**: Animated dog character that follows NavMesh path, waits for user if too far, handles NavMeshLinks
- **ColmapToUnityTransformer**: Converts COLMAP photogrammetry coordinates to Unity world space using calibrated transform

### Data Models
```csharp
ObjectItem {
    id, user_name, image_path, object_label,
    object_colors (List<string>), description,
    anchor_x/y/z, created_at
}
```

## Key Scenes

- `Assets/Scenes/kitFind.unity` - Main application scene

## Build Commands

Open project in Unity Hub (Unity 6), then:
- **Play Mode**: Ctrl+P / Cmd+P
- **Build**: File → Build Settings → Build

## Code Patterns

- Korean comments throughout codebase
- Server URL hardcoded in multiple files: `http://192.168.0.146:8000`
- Coroutine-based async operations with callback patterns
- TextMeshPro (TMPro) for UI text elements
