# Darkwave

A Unity 3D survival horror shooter where players fight off waves of enemies during intense night cycles in a post-apocalyptic world.

## Game Overview

**Darkwave** is a first-person survival shooter featuring:
- **Day/Night Cycle**: Strategic gameplay with different mechanics during day and night
- **Enemy Wave System**: Multiple enemy types with unique behaviors and abilities
- **Survival Mechanics**: Health management and safe zone objectives

## Gameplay Features

### Core Mechanics
- **First-Person Shooting**: Raycast-based weapon system with multiple enemy interactions
- **Day/Night Cycle**: Dynamic lighting and fog with time-based gameplay changes
- **Enemy AI**: Three distinct enemy archetypes with unique behaviors
- **Health System**: Player damage/healing with UI feedback and respawn mechanics

### Enemy Types

2. **Tank Enemy** - Heavy armored threat
   - Health: 300 HP
   - Speed: 1.2 units (slower)
   - Damage: 60 (high damage)
   - Range: 4.5 units
   - Special: Charge attack ability

3. **Fast Enemy** - Quick and agile
   - Health: 50 HP (low)
   - Speed: 8 units (very fast)
   - Damage: 15 (low)
   - Range: 2.2 units
   - Special: Dodge ability

### Weapons System
- **AK47**: Primary weapon with pickup mechanics
- **Ammo Management**: Limited ammunition system
- **Weapon Switching**: Inventory-based weapon management
- **Damage System**: Configurable damage per weapon type

## Win Conditions

Players can achieve victory by:
- Reaching and capturing designated Safe Zone

## Technical Features

### Unity Systems
- **Universal Render Pipeline (URP)**: Optimized graphics pipeline
- **Navigation Mesh**: AI pathfinding for enemy movement
- **Animation System**: Full animator support for characters
- **Physics System**: Rigidbody-based interactions
- **Audio System**: 3D spatial audio with SoundManager

### WebGL Optimizations
- **Memory Management**: Optimized for 256MB browser limits
- **Quality Settings**: Dedicated WebGL quality preset
- **Compression**: Gzip compression enabled for smaller builds
- **Resolution Scaling**: 1280x720 default for optimal performance

## Project Structure

```
Assets/
├── Scripts/              # Core game logic
│   ├── Enemy/            # Enemy AI systems
│   ├── Player/           # Player controllers
│   ├── Managers/         # Game management systems
│   └── UI/               # User interface scripts
├── Prefabs/              # Reusable game objects
├── Scenes/               # Game scenes
├── Materials/            # Visual materials
├── Models/               # 3D models and assets
├── Sounds/               # Audio files
├── Animations/           # Animation controllers
└── Settings/             # Project settings
```

### Key Scripts
- **Weapon.cs**: Weapon firing mechanics with multiple enemy type support
- **Inventory.cs**: Weapon management and switching system
- **Enemy Scripts**: SimpleEnemy.cs, TankEnemy.cs, FastEnemy.cs
- **PlayerHealth.cs**: Health management with UI integration
- **GameManager.cs**: Game state and win condition management
- **ScoreManager.cs**: Point system and progression tracking
- **DayNightCycle.cs**: Dynamic lighting and time management

## Getting Started

### Prerequisites
- Unity 2022.3 or later
- Windows 10/11 (development)
- Modern web browser with WebGL 2.0 support (deployment)

### Setup Instructions

1. **Clone/Download** the project to your local machine
2. **Open in Unity**: Launch Unity Hub and open the project folder
3. **Platform Configuration**: 
   - Go to File > Build Settings
   - Select WebGL platform
   - Click "Switch Platform"
4. **Quality Settings**: Ensure "WebGL Optimized" quality level is selected
5. **Build**: Click "Build" or "Build and Run" to generate WebGL output

### First Time Setup
1. Ensure all enemy prefabs have required components:
   - NavMeshAgent (for pathfinding)
   - Animator (for animations)
   - Colliders (for physics)
   - Enemy-specific scripts
2. Verify UI references are properly assigned in PlayerHealth component
3. Check that spawn points are configured for enemy spawning

## Controls

- **Mouse**: Look around / Aim
- **WASD**: Movement
- **Left Click**: Fire weapon
- **E**: Interact / Pickup weapons
- **Escape**: Pause menu

## Development

### Build Profiles
- **WebGL Profile**: Optimized for web deployment
- **Development Build**: Enable for debugging and profiling

### Known Optimizations
- **Memory**: 256MB limit with dynamic growth
- **Graphics**: WebGL 2.0 with 1.0 fallback
- **Resolution**: 1280x720 for optimal performance
- **Shaders**: URP shaders included for WebGL compatibility
- **Audio**: Compressed formats for faster loading


### Common Issues

**Magenta/Pink Textures**

**Missing UI Elements**

**Enemy AI Issues**
- Confirm NavMesh is properly baked for the scene
- Check enemy prefab configurations
- Verify Player tag is assigned to player GameObject

**Performance Issues**
- Use WebGL quality preset
- Reduce texture sizes if needed
- Monitor browser memory usage

## Credits

### Assets Used
- **AllSkyFree**: Skybox materials
- **Low Poly Weapons VOL.1**: Weapon models
- **Polytope Studio**: Additional models
- **StarterAssets**: Character controller components
- **TextMesh Pro**: UI text rendering

### Development
- Unity Universal Render Pipeline
- NavMesh AI system
- Built-in Animation system

## Version History

### v1.0 (Current)
- Initial release with core gameplay mechanics
- Two enemy types with unique behaviors
- WebGL optimization and deployment
- Day/night cycle system
- Health and scoring systems