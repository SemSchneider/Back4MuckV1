# Simple Enemy Setup Guide

## Files Created:
1. **SimpleEnemy.cs** - Main enemy AI script
2. **PlayerHealth.cs** - Player health system for taking damage
3. **EnemySpawner.cs** - System to spawn enemies in the game

## How to Set Up the Enemy:

### Step 1: Create Enemy Prefab
1. Create a new GameObject in your scene
2. Name it "SimpleEnemy"
3. Add these components:
   - **NavMeshAgent** (for pathfinding)
   - **CapsuleCollider** (for collision detection)
   - **Rigidbody** (for physics)
   - **SimpleEnemy** script
   - **Animator** (optional, for animations)

### Step 2: Configure NavMeshAgent
- Set **Speed** to 3.5
- Set **Stopping Distance** to 1.5
- Enable **Auto Braking**

### Step 3: Configure SimpleEnemy Script
- **Health**: 100
- **Move Speed**: 3.5
- **Detection Range**: 10
- **Attack Range**: 2
- **Attack Damage**: 25
- **Attack Cooldown**: 1.5

### Step 4: Set Up Player Health
1. Add **PlayerHealth** script to your player GameObject
2. Configure the health settings
3. Set up UI elements (health bar, health text) if desired

### Step 5: Create Enemy Spawner
1. Create empty GameObject named "EnemySpawner"
2. Add **EnemySpawner** script
3. Assign the enemy prefab
4. Configure spawn settings:
   - **Max Enemies**: 5
   - **Spawn Radius**: 20
   - **Spawn Interval**: 10 seconds
   - **Min Distance From Player**: 15

### Step 6: Set Up NavMesh
1. Select your terrain/ground objects
2. In Inspector, check **Navigation Static**
3. Go to **Window > AI > Navigation**
4. Click **Bake** to generate NavMesh

### Step 7: Tag Your Player
Make sure your player GameObject has the tag "Player" so enemies can find it.

## Enemy Behavior:
- **Patrol**: Enemies will stand still when player is not in detection range
- **Chase**: When player enters detection range, enemy will move towards player
- **Attack**: When close enough, enemy will stop and attack
- **Death**: When health reaches 0, enemy will die and be destroyed

## Optional Enhancements:
- Add enemy models/meshes
- Create animations (walk, attack, death)
- Add sound effects
- Create different enemy types
- Add enemy health bars
- Implement enemy drops/loot

## Testing:
- Place the enemy spawner in your scene
- Make sure NavMesh is baked
- Run the game and enemies should spawn and chase the player
- Test combat by shooting enemies and letting them attack you
