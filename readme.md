# Interactive Organisms Viewer

This Unity3D application is an interactive image viewer designed to showcase object-oriented programming, data handling, and UI/UX design. It features a menu scene and a game scene where users can explore biological organisms by interacting with labeled parts to view detailed information.

## Features

*   **Menu Scene:** Simple start screen with navigation and visual elements.
*   **Dynamic Data:** Organism images and label data loaded dynamically from JSON files via URLs.
*   **Interactive Labels:** Clickable labels reveal detailed information.
*   **Info Panel:** Dedicated UI for displaying label details.
*   **Organism Selection:** Dropdown to switch between organisms.
*   **Scene Transitions:** Smooth scene changes.
*   **Persistent Navigation:** Consistent Home/Quit buttons in game and menu scenes.
*   **Internet Connectivity Handling:** Gracefully handles lack of internet connection by displaying a default image and providing user feedback.

## Code Structure

The project uses a modular structure with several key scripts and assets:

*   **`MenuManager.cs`:** Controls the main menu logic.
*   **`GameManager.cs`:** Manages the game scene, data loading, label interaction, and UI. **Includes logic for checking internet connectivity and handling image loading failures.**
*   **`DataManager.cs`:** Handles loading organism data from JSON, including image URLs. (Singleton)
*   **`NavigationController.cs`:** Manages persistent Home/Quit buttons. (Singleton)
*   **`SceneTransitionManager.cs`:** Handles scene transitions. (Singleton)
*   **`OrganismLabelManager.cs`:** Helps locate label positions for specific organisms.
*   **`LabelPositionMarker.cs`:** Defines precise label placement points in the scene hierarchy.
*   **`LabelConnection.cs`:** Draws lines from labels to points using `UILineRenderer`.
*   **`UILineRenderer.cs`:** Utility for drawing UI lines.
*   **`AudioManager.cs`:** Manages basic audio throughout the game. (Singleton)
*   **JSON Files (`amoeba.json`, etc.):** External organism and label data, with image URLs at the top of each file.
*   **Prefabs:** **`Labeled info` and `Labeled line` prefabs are created for reusable UI elements**, simplifying scene setup.
*   **Code Commenting:** All functions and significant code sections are properly commented for clarity and ease of understanding the code structure.

## Design Choices & Justifications

*   **Dynamic JSON Data:** Data is loaded from external JSON files for easy updates and additions of new organisms without code changes. This promotes flexibility and maintainability. **New organisms can be added by dragging and dropping the main image into the game scene "Image Container -> Amoeba_Image gameobejct" and setting label positions easily through the Unity hierarchy "OrganismConfigurations" gameobejct in the game scene using `LabelPositionMarker` components.**
*   **Modular Scripts (OOP):** Code is split into manager scripts with single responsibilities (e.g., `DataManager`, `GameManager`). This improves organization, readability, and testability, adhering to OOP principles. Singletons are used for easily accessible global managers.
*   **Label Positioning:** Using `LabelPositionMarker` GameObjects in the scene allows for precise visual control and easy adjustment of label placement relative to the image within the Unity editor, complementing data-driven positioning.
*   **Internet Connectivity Handling:** Implementing checks for internet availability ensures a more robust user experience. If online resources (like images) cannot be loaded, the application falls back to displaying a default image and provides clear feedback to the user via a popup, preventing the application from appearing broken.
*   **UI/UX:**
    *   A dropdown is used for organism selection for **easy access to all organisms at once.**
    *   Interactive labels and a dedicated info panel provide a clear and intuitive way to explore organism details. **The bubble-like info panel and its button visually indicate interactivity.**
    *   **Consistent placement of Quit/Home navigation buttons** across scenes enhances user familiarity.

## Creative Implementations

*   **Scene Transition Animation:** A zoom-in animation occurs when entering the game scene from the menu, providing a smooth and engaging visual transition.
*   **Organism Switching Animation:** A natural-feeling slide/swipe animation transitions between organisms, visually suggesting browsing and providing a polished content change.
*   **Menu Scene Visuals:** Animated fishes with splash particle effects add visual interest and activity to the menu scene.
*   **Image Shader:** A custom shader and material properties are used to hide the white background around images loaded from URLs**, making them blend more naturally with the scene.

## How to Run

1.  Open the project in Unity3D.
2.  Add "Menu" and "Game" scenes to Build Settings.
3.  Open the "Menu" scene.
4.  Press Play in the Unity editor.
5.  Click the Play button in the Menu Scene, then use the dropdown to switch organisms and click labels for info.
6.  Use Home/Quit buttons for navigation (Home in game scene, Quit in built app).