using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace UAI
{
    public static class AssetCreatorUtility
    { 
        public static string GetPrompt(string category, string assetName, string styleName)
        {
            if (categorizedGameArtPresets.ContainsKey(category) && 
                categorizedGameArtPresets[category].ContainsKey(assetName) &&
                gameArtStyles.ContainsKey(styleName))
            {
                string assetTemplate = categorizedGameArtPresets[category][assetName];
                string styleDescription = gameArtStyles[styleName];
                
                return assetName + " " + assetTemplate.Replace("[STYLE_HERE]", styleDescription);
            }
            
            return "";
        }
        public static Dictionary<string, string> gameArtStyles = new Dictionary<string, string>
        {
            { "PixelArt", "in a classic pixel art style, clear silhouettes, and crisp edges. Each pixel should be deliberately placed with a 32×32 resolution. Include subtle shading for depth while maintaining the retro aesthetic. Transparent background." },

            { "Cartoon", "in a clean and colorful vector-art style with bold outlines, flat colors, and minimal shading. Exaggerated proportions and simplified forms that are bright and playful, suited for a mobile game or children's game. Transparent background." },

            { "Fantasy", "in a rich, painterly fantasy style with detailed textures and atmospheric lighting. Include magical elements like glowing runes, mystical auras, or enchanted materials. Design should evoke a sense of wonder and adventure with a slightly exaggerated, heroic aesthetic. Transparent background." },

            { "SciFi", "in a sleek, futuristic style with clean lines, metallic surfaces, and technological details. Include holographic elements, energy effects, and subtle ambient lighting. Color palette should feature cool blues, teals, and accent colors like neon orange or purple. Transparent background." },

            { "Horror", "in a dark, atmospheric style with high contrast lighting and gritty textures. Incorporate unsettling details, weathered surfaces, and environmental storytelling elements. Color palette should be desaturated with occasional accent colors (particularly reds) for emphasis. Transparent background." },

            { "Casual", "in a bright, approachable style with simple shapes, clean designs, and cheerful colors. Emphasis on readability and instant visual appeal. Include subtle highlights, soft shadows, and smooth gradients. Designed to be attractive and accessible to a wide audience. Transparent background." },

            { "Realistic", "with detailed texturing, accurate proportions, and realistic lighting. Include careful attention to material properties, environmental effects, and natural wear. Subtle color grading should enhance the mood while maintaining believability. Design should prioritize immersion and authenticity. Transparent background." },

            { "Isometric", "in an isometric perspective (2:1 ratio) with clear edges and consistent 30-degree angle. Include detailed facades with depth cues and consistent light source from upper left. Design should accommodate tile-based assembly with clean connecting edges. Transparent background." },

            { "HandDrawn", "with visible brush or pen strokes, organic lines, and a slightly imperfect aesthetic. Include texture in the fills, varied line weights, and a warm color palette. Design should feel crafted and artisanal rather than digital or precise. Transparent background." },

            { "Minimalist", "using simple geometric forms, limited color palette (3-5 colors maximum), and negative space. Design should communicate function and character through silhouette and basic shapes. Avoid unnecessary details while maintaining clarity and strong visual impact. Transparent background." },

            { "3D digital illustration", "in a 3D-rendered digital illustration in a stylized cartoon art style. Use bright, saturated colors and clean, soft lighting. Surfaces should feature smooth, plastic-like or painted materials with subtle texture detail, resembling hand-crafted toys or animation-style assets. Outlines are minimal or absent, but forms are clearly defined with bold contrasts and soft shadows. Include exaggerated proportions and whimsical design elements to give a playful, fantasy-inspired tone. The render should mimic the look of professional 3D animation concept art or game assets. Maintain a balanced, isometric or orthographic camera angle. Transparent or blank background." }
        };
        public static Dictionary<string, Dictionary<string, string>> categorizedGameArtPresets = new Dictionary<string, Dictionary<string, string>>
        {
            { "Tileset", new Dictionary<string, string>
                {
                    { "Overworld Tileset",
                        "[STYLE_HERE] The tileset should include modular tiles for:\n" +
                        "- Grass (with edge variants for cliffs and rivers)\n" +
                        "- Dirt paths (with corners and straight segments)\n" +
                        "- River water with animated flow options\n" +
                        "- Wooden bridges (horizontal and vertical)\n" +
                        "- Cliffs with shadows and stairs\n" +
                        "- Trees (separate top and trunk for layering)\n" +
                        "- Bushes, flowers, mushrooms, and tree stumps\n" +
                        "- Village buildings (roof, wall, door, and window tiles separately)\n" +
                        "- Mailbox, wooden signpost, and lamp posts\n" +
                        "- Light overlay tiles for creating lit areas at night\n" +
                        "Tiles should be grid-aligned and modular for seamless tiling, each tile 32×32 pixels. Designed for top-down perspective."
                    },
                    { "Interior Tileset",
                        @"[STYLE_HERE] The tileset should include modular tiles for:
- Wooden floor tiles (with edge variants for corners and doorways)
- Stone floor tiles (with cracks and dirt variations)
- Wall tiles (wooden, stone, and brick with window cutouts)
- Ceiling tiles (wooden beams, chandeliers)
- Furniture: tables, chairs, beds, bookshelves
- Decorative items: paintings, potted plants, rugs
- Doors (open and closed variants)
- Windows (with curtains and shutters)
- Stairs (up and down)
- Light sources: candles, lanterns
Tiles should be grid-aligned and modular for seamless tiling, each tile 32×32 pixels. Designed for top-down perspective."
                    },
                    { "Dungeon Tileset",
                        @"[STYLE_HERE] The tileset should include modular tiles for:
- Stone floor tiles (with cracks and dirt variations)
- Wall tiles (stone with moss and cracks)
- Ceiling tiles (stone arches, stalactites)
- Decorative items: torches, cobwebs, skulls
- Furniture: tables, chairs, beds, bookshelves
- Doors (wooden and metal with lock variants)
- Windows (with bars and shutters)
- Stairs (up and down)
- Light sources: candles, lanterns
Tiles should be grid-aligned and modular for seamless tiling, each tile 32×32 pixels. Designed for top-down perspective."
                    },
                    { "Nature Tileset",
                        @"[STYLE_HERE] The tileset should include modular tiles for:
- Grass (with edge variants for cliffs and rivers)
- Dirt paths (with corners and straight segments)
- River water with animated flow options
- Wooden bridges (horizontal and vertical)
- Cliffs with shadows and stairs
- Trees (separate top and trunk for layering)
- Bushes, flowers, mushrooms, and tree stumps
- Decorative elements: rocks, fallen logs, stumps
- Small animals or creatures as ambient life
- Light overlay tiles for creating lit areas at night
Tiles should be grid-aligned and modular for seamless tiling, each tile 32×32 pixels. Designed for top-down perspective."
                    },
                    { "City Tileset",
                        @"[STYLE_HERE] The tileset should include modular tiles for:
- Cobblestone streets (with edge variants for corners and intersections)
- Brick buildings (with window cutouts)
- Decorative items: street lamps, benches, fountains
- Marketplace stalls and signs
- Various roof types and architectural details
- Doors (open and closed variants)
- Windows (with curtains and shutters)
- Stairs (up and down)
- Light sources and shadows
Tiles should be grid-aligned and modular for seamless tiling, each tile 32×32 pixels. Designed for top-down perspective."
                    },
                    { "Space Tileset",
                        @"[STYLE_HERE] The tileset should include modular tiles for:
- Space station walls (with window cutouts)
- Futuristic floor tiles (with glowing patterns)
- Decorative items: holographic displays, control panels
- Tech furniture: terminals, sleeping pods, medical bays
- Doors (automatic sliding variants)
- Windows (with energy shields or views to space)
- Stairs and elevators
- Light sources: neon lights, holograms
Tiles should be grid-aligned and modular for seamless tiling, each tile 32×32 pixels. Designed for top-down perspective."
                    },
                    { "Underwater Tileset",
                        @"[STYLE_HERE] The tileset should include modular tiles for:
- Ocean floor (with sand and coral variations)
- Decorative items: seaweed, rocks, shells
- Underwater structures: caves, ruins, modern facilities
- Flora: coral formations, anemones, underwater plants
- Ambient creatures: small fish, crabs, jellyfish
- Doors (underwater sealed variants)
- Windows (with bubble effects)
- Vertical movement paths
- Light sources: bioluminescent plants, artificial lights
Tiles should be grid-aligned and modular for seamless tiling, each tile 32×32 pixels. Designed for top-down perspective."
                    },
                    { "Platformer Tileset",
                        @"[STYLE_HERE] Each tile should be modular and fit a grid system. 
The tileset should include:
- Grass platform tiles (top, corners, inner corners, sides)
- Dirt blocks with variation: plain, with stones, with roots or flowers
- Floating brick tiles (for mid-air platforms)
- Wooden crate tiles (top, side views)
- Wooden ladder tiles (vertical and top entry points)
- Tree parts: canopy tiles, trunk tiles
- Background scenery: layered mountains, clouds
- Collectible items: coins, gems, power-ups
- Props: signs, checkpoints, small plant decorations
Designed for a side-scrolling 2D platformer."
                    }
                }
            },
            { "Character", new Dictionary<string, string>
                {
                    { "Hero Character",
                        @"[STYLE_HERE] Full-body character in a dynamic action pose. Should include:
- Class variants (warrior with armor; rogue with hood; mage with robes)
- Proportions appropriate to the style
- Signature weapon or item (sword, dagger, staff) with style-appropriate effects
- Directional lighting and shading to emphasize form
- Plain or transparent background for easy compositing"
                    },
                    { "NPC Character",
                        @"[STYLE_HERE] Full-body non-player character with neutral or welcoming pose. Should include:
- Profession variants (merchant, villager, guard, elder)
- Appropriate clothing and accessories for their role
- Distinctive features that communicate personality
- Simple props or tools related to their function
- Neutral expression suitable for dialogue scenes
- Plain or transparent background for easy compositing"
                    },
                    { "Enemy Character",
                        @"[STYLE_HERE] Full-body adversary character in a threatening or combat pose. Should include:
- Type variants (humanoid, beast, elemental, undead)
- Threatening or aggressive posture
- Distinctive features that communicate danger level
- Weapons, claws, or magical effects as appropriate
- Plain or transparent background for easy compositing"
                    }
                }
            },
            { "Environment", new Dictionary<string, string>
                {
                    { "Forest Scene",
                        @"[STYLE_HERE] Lush forest environment with atmospheric elements. Should feature:
- Dense tree canopy with dappled light
- Forest floor with undergrowth, rocks, and fallen logs
- Possible path or clearing as focal point
- Small details: mushrooms, flowers, moss
- Ambient particles: dust motes, leaves, insects
- Multi-layered parallax elements for depth"
                    },
                    { "Castle Scene",
                        @"[STYLE_HERE] Medieval castle setting with architectural details. Should feature:
- Stone walls with appropriate weathering
- Towers, battlements, and appropriate entryways
- Interior or exterior viewpoint with appropriate furnishings/elements
- Decorative elements: banners, torches, statues
- Atmospheric effects: light shafts, torch glow, shadows
- Multi-layered parallax elements for depth"
                    },
                    { "Dungeon Scene",
                        @"[STYLE_HERE] Underground dungeon environment with ominous atmosphere. Should feature:
- Stone walls with appropriate weathering, moss, or water damage
- Corridors, chambers, or pit configurations
- Decorative elements: chains, broken furniture, remains
- Light sources: torches, braziers, magical elements
- Atmospheric effects: fog, dust, dripping water
- Multi-layered parallax elements for depth"
                    },
                    { "City Scene",
                        @"[STYLE_HERE] Urban environment with architectural variety. Should feature:
- Buildings with appropriate cultural/period styling
- Street level with appropriate paving and details
- Decorative elements: signs, lampposts, market stalls
- Background elements: distant structures, skyline
- Environmental effects: appropriate weather, lighting
- Multi-layered parallax elements for depth"
                    },
                    { "Sci-Fi Scene",
                        @"[STYLE_HERE] Futuristic environment with technological elements. Should feature:
- Sleek or industrial architecture as appropriate
- Technical details: control panels, screens, machinery
- Energy sources, conduits, or propulsion elements
- Decorative elements: signage, holographic displays
- Lighting effects: neon, screens, energy sources
- Multi-layered parallax elements for depth"
                    },
                    { "Overworld Map",
                        @"[STYLE_HERE] Top-down stylized map with varied terrain. Include:
- Terrain types: mountains, rivers, forests, deserts, swamps
- Landmarks: villages, castles, dungeons, bridges
- Connecting roads or paths, optional grid overlay
- Consistent visual style across all regions
- Appropriate top-down perspective and scale"
                    }
                }
            },
            { "Creature", new Dictionary<string, string>
                {
                    { "Fantasy Beast",
                        @"[STYLE_HERE] Full-body creature design with distinctive features. Include:
- Type variants: dragon, griffin, or mythical beast
- Unique color palette appropriate to the style
- Textural details: scales, feathers, or fur as appropriate
- Dynamic pose showcasing character
- Plain background for clarity"
                    },
                    { "Monster Design",
                        @"[STYLE_HERE] Full-body hostile creature with threatening aspects. Include:
- Type variants: elemental being, aberration, corrupted entity
- Distinctive silhouette and recognizable features
- Appropriate texture details: slime, rock, corruption
- Aggressive or lurking pose
- Plain background for clarity"
                    },
                    { "Companion Creature",
                        @"[STYLE_HERE] Full-body friendly creature with appealing features. Include:
- Type variants: familiar, pet, or magical helper
- Expressive features that convey personality
- Appropriate texture details for the creature type
- Friendly or helpful pose
- Possible magical or special effect elements
- Plain background for clarity"
                    }
                }
            },
            { "UI", new Dictionary<string, string>
                {
                    { "UI Component Set",
                        @"[STYLE_HERE] Modular UI components with consistent design language. Include:
- Button states (normal, hover, pressed)
- Frames for portraits, inventory, and information
- Bars for health, mana, experience, or other stats
- Icons container placeholders
- Text box or dialog window
- Consistent corner styling and borders
- Color variations for different game states
- Transparent background for element flexibility"
                    },
                    { "Game Icon Set",
                        @"[STYLE_HERE] Collection of matching interface icons in a consistent style. Include:
- Basic actions: attack, defend, interact, move
- Item categories: weapons, armor, potions, scrolls
- Status effects: buffs, debuffs, altered states
- Navigation elements: map, quest, inventory, character
- Bold, recognizable silhouettes optimized for small sizes
- Consistent line weight and detail level
- Transparent background"
                    },
                    { "HUD Layout",
                        @"[STYLE_HERE] Complete heads-up display layout with integrated components. Include:
- Health and resource indicators
- Minimap or position indicator
- Action buttons or ability slots
- Score or currency display
- Contextual prompt area
- Consistent styling across all elements
- Clear information hierarchy
- Semi-transparent backgrounds where appropriate"
                    }
                }
            },
            { "Items", new Dictionary<string, string>
                {
                    { "Weapon Set",
                        @"[STYLE_HERE] Collection of thematically consistent weapons. Include:
- Melee variants: sword, axe, mace, dagger
- Ranged variants: bow, crossbow, staff, wand
- Different quality/rarity levels with appropriate detailing
- Consistent material rendering and highlight style
- Side view or angled presentation
- Transparent background"
                    },
                    { "Armor Set",
                        @"[STYLE_HERE] Collection of matching armor pieces. Include:
- Helmet, chest piece, gloves, boots
- Different quality/rarity levels with appropriate detailing
- Consistent material rendering and highlight style
- Front view or angled presentation
- Transparent background"
                    },
                    { "Potion Set",
                        @"[STYLE_HERE] Collection of magical or alchemical containers. Include:
- Different shapes: vials, flasks, bottles, jars
- Varied liquid colors for different effects
- Stoppers, labels, or distinguishing features
- Possible magical effects or glows
- Front view presentation with subtle angle
- Transparent background"
                    },
                    { "Treasure Set",
                        @"[STYLE_HERE] Collection of valuable loot items. Include:
- Coins, gems, jewelry
- Treasure chests (closed and open states)
- Special artifacts or relics
- Appropriate material rendering (metal, gem)
- Subtle highlights or glints to suggest value
- Transparent background"
                    },
                    { "Magic Spell Effect",
                        @"[STYLE_HERE] Centered visual effect element in mid-cast. Include:
- Element variants: fire, ice, lightning, nature, arcane
- Multi-layer particle details appropriate to element
- Core and outer effect distinction
- Suggestion of motion or energy
- Appropriate lighting effects
- Transparent background"
                    }
                }
            },
            { "Vehicle", new Dictionary<string, string>
                {
                    { "Land Vehicle",
                        @"[STYLE_HERE] Ground transportation design with distinctive silhouette. Include:
- Type variants: cart, carriage, mechanical transport
- Appropriate wheels, runners, or propulsion
- Seating or cargo area
- Thematic details matching the style context
- Side view or 3/4 angle presentation
- Transparent background"
                    },
                    { "Water Vehicle",
                        @"[STYLE_HERE] Watercraft design with appropriate details. Include:
- Type variants: boat, ship, raft, or fantasy equivalent
- Hull, deck, and propulsion elements
- Sails, oars, or mechanical components as appropriate
- Thematic details matching the style context
- Side view or 3/4 angle presentation
- Transparent background"
                    },
                    { "Air Vehicle",
                        @"[STYLE_HERE] Flying transport with distinctive features. Include:
- Type variants: airship, balloon, magical platform
- Propulsion or lifting mechanisms
- Passenger or cargo compartments
- Thematic details matching the style context
- Side view or 3/4 angle presentation
- Transparent background"
                    }
                }
            },
            { "Texture", new Dictionary<string, string>
                {
                    { "Natural Material",
                        @"[STYLE_HERE] Seamless texture for natural surfaces. Include:
- Type variants: wood, stone, grass, water
- Subtle detail variation to avoid obvious tiling
- Appropriate bump/height suggestion through shading
- Edge-matched borders for perfect tiling
- Consistent lighting direction
- 512×512 pixel dimensions or appropriate resolution"
                    },
                    { "Manufactured Material",
                        @"[STYLE_HERE] Seamless texture for crafted surfaces. Include:
- Type variants: brick, tile, metal, fabric
- Regular pattern with subtle imperfections
- Appropriate material properties through shading
- Edge-matched borders for perfect tiling
- Consistent lighting direction
- 512×512 pixel dimensions or appropriate resolution"
                    },
                    { "Decorative Pattern",
                        @"[STYLE_HERE] Seamless ornamental texture for details. Include:
- Type variants: carpet design, wallpaper, engraving
- Culturally appropriate motifs
- Regular repeat with balanced elements
- Appropriate material suggestion through shading
- Edge-matched borders for perfect tiling
- 512×512 pixel dimensions or appropriate resolution"
                    }
                }
            },
            { "Effect", new Dictionary<string, string>
                {
                    { "Explosion",
                        @"[STYLE_HERE] Dynamic burst effect with appropriate energy. Include:
- Center core and expanding outer elements
- Stage variants: initial, peak, dissipation
- Particle details and light emission
- Suggested motion through shape
- Color variations appropriate to type (fire, magic, etc.)
- Transparent background"
                    },
                    { "Magic Aura",
                        @"[STYLE_HERE] Character enhancement effect. Include:
- Element variants: fire, ice, lightning, arcane
- Layer structure: close body layer and outer particles
- Movement suggestion through shape design
- Appropriate glow and energy representation
- Color variations matching elemental theme
- Transparent background"
                    },
                    { "Environmental Effect",
                        @"[STYLE_HERE] Atmospheric particle system. Include:
- Type variants: rain, snow, leaves, dust, embers
- Multiple particle sizes and densities
- Directional suggestion through shape
- Appropriately subtle or dramatic presence
- Color variations fitting the effect type
- Transparent background"
                    }
                }
            },

    { "Building", new Dictionary<string, string>
        {
            // House variants
            { "House / Residential House",
                @"[STYLE_HERE] Exterior A single‐family residential house. Should include:
- Modular wall segments (wood, brick, or stone)
- Roof variants (gabled, hipped, flat)
- Doors and windows as separate tiles
- Chimney or vent elements
- Porch or stoop pieces
- Decorative trim: shutters, flower boxes, gutters
Designed for top‐down or isometric perspective." },
            { "House / Suburban Home",
                @"[STYLE_HERE] Exterior A cozy suburban home with yard. Should include:
- Modular siding and brick wall segments
- Pitched roof with garage door module
- Front yard lawn, sidewalk, and driveway tiles
- Front porch, steps, and railing pieces
- Window boxes, mailbox, and lamppost props
Designed for top‐down or isometric perspective." },

            // Townhouse variants
            { "Townhouse / Townhouse Row",
                @"[STYLE_HERE] Exterior Multi‐story attached townhouses. Should include:
- Repeating façade modules with variant details
- Shared wall segments
- Individual door and window units
- Continuous eave and gutter pieces
- Balcony or fire escape elements
- Decorative cornices and moldings
Designed for urban street scenes." },
            { "Townhouse / Victorian Townhouse Row",
                @"[STYLE_HERE] Exterior Ornate Victorian‐style rowhouses. Should include:
- Intricate brick and wood façade panels
- Bay window modules with stained glass
- Decorative iron railings and balconies
- Mansard or steep‐pitched roof segments
- Front stoop with railing and lamp props
Designed for urban street scenes." },

            // Apartment variants
            { "Apartment / Apartment Complex",
                @"[STYLE_HERE] Exterior Multi‐unit residential building. Should include:
- Ground‐floor entrance lobby with canopy
- Stacked floor modules with balcony sections
- Window arrays (single, double, bay)
- Stairwell and elevator shaft details
- Rooftop HVAC or water tank elements
- Ground‐level planters or benches
Designed for top‐down or isometric perspective." },
            { "Apartment / High‐Rise Apartment Tower",
                @"[STYLE_HERE] Exterior Modern high‐rise apartment tower. Should include:
- Glass and concrete curtain wall panels
- Recessed balcony modules
- Rooftop garden and mechanical units
- Entrance canopy and revolving door tiles
- Sidewalk plaza with benches and planters
Designed for city skyline scenes." },

            // Cottage variants
            { "Cottage / Cottage",
                @"[STYLE_HERE] Exterior Quaint rural cottage. Should include:
- Stone or timber‐frame walls with mortar detail
- Thatched or shingled roof pieces
- Small windows with mullions
- Rounded door variants
- Flower beds, trellises, and climbing vines
Designed for charming village settings." },
            { "Cottage / Forest Cottage",
                @"[STYLE_HERE] Exterior Woodland‐style cottage. Should include:
- Mossy log and timber wall segments
- Overgrown shingle roof tiles
- Fern, mushroom, and vine props
- Stone chimney with smoke plume module
- Leaf‐litter ground overlay tiles
Designed for enchanted forest scenes." },

            // Castle Keep variants
            { "Castle Keep / Medieval Castle Keep",
                @"[STYLE_HERE] Exterior Central tower of a fortified castle. Should include:
- Thick stone wall modules (with moss and weathering)
- Battlement segments and crenellations
- Arrow slit and portcullis units
- Wooden drawbridge and gate pieces
- Watchtower roof segments
- Torches or braziers as accent props
Designed for top‐down or isometric RPG maps." },
            { "Castle Keep / Watch Keep Outpost",
                @"[STYLE_HERE] Exterior Small stone watch keep. Should include:
- Short curtain wall segments with arrow slits
- Single battlemented tower module
- Wooden hoarding and ladder pieces
- Flagpole and signal fire props
- Gravel path and outbuilding foundation tiles
Designed for frontier defense maps." },

            // Skyscraper variants
            { "Skyscraper / Skyscraper",
                @"[STYLE_HERE] Exterior Modern high‐rise building. Should include:
- Glass curtain wall panels
- Metal cladding modules
- Window arrays with reflective details
- Rooftop mechanical and antenna units
- Lobby entrance canopy and revolving door
- Sidewalk segment with planters and benches
Designed for city skyline scenes." },
            { "Skyscraper / Corporate Glass Tower",
                @"[STYLE_HERE] Exterior Sleek corporate office tower. Should include:
- Floor‐to‐ceiling tinted glass panels
- Cantilevered balcony and atrium modules
- Rooftop helipad and HVAC units
- Main plaza with water feature and seating
- Entrance canopy with logo sign tile
Designed for modern urban environments." },

            // Factory / Warehouse variants
            { "Factory / Warehouse / Factory / Warehouse",
                @"[STYLE_HERE] Exterior Industrial building. Should include:
- Large brick or metal siding panels
- Rolling door and loading dock modules
- Chimney stacks and vent pipes
- Roof skylight and HVAC units
- Exterior piping and conduit details
- Crate and barrel props
Designed for urban or rural industrial zones." },
            { "Factory / Warehouse / Abandoned Industrial Warehouse",
                @"[STYLE_HERE] Exterior Derelict warehouse structure. Should include:
- Rusted metal siding and broken brick walls
- Collapsed roof beam and gaping skylights
- Overturned crates and debris piles
- Vegetation overgrowth modules
- Graffiti and weather stain props
Designed for post‐apocalyptic or urban exploration settings." },

            // Temple variants
            { "Temple / Shrine / Temple / Shrine",
                @"[STYLE_HERE] Exterior Religious or cultural temple. Should include:
- Ornate column and beam segments
- Tiered roof modules with decorative tiles
- Altar or inner sanctum entrance
- Statues, lanterns, and incense burners
- Stairs and platform pieces
- Wall relief or mosaic patterns
Designed for top‐down or isometric perspective." },
            { "Temple / Shrine / Mountain Temple",
                @"[STYLE_HERE] Exterior Cliffside mountain temple. Should include:
- Weathered stone and wood wall segments
- Multi‐tier pagoda roof modules
- Hanging prayer flags and lantern props
- Stone stairway and terrace pieces
- Rock ledge and cliff foundation tiles
Designed for elevated or mountainous maps." },

            // Lighthouse variants
            { "Lighthouse / Lighthouse",
                @"[STYLE_HERE] Exterior Coastal tower structure. Should include:
- Cylindrical wall segments with slit windows
- Tapered roof or lantern room pieces
- Railings and catwalk modules
- Light beacon and lens details
- Stone foundation and steps
Designed for shoreline scenes." },
            { "Lighthouse / Harbor Lighthouse",
                @"[STYLE_HERE] Exterior Pier‐side lighthouse. Should include:
- Square brick or concrete wall segments
- Elevated lantern room deck and railing
- Dockside platform and mooring cleat tiles
- Signal horn and fog bell props
- Water edge overlay with wave tile variants
Designed for harbor settings." },

            // Ruin variants
            { "Ruined Building / Ruined Building",
                @"[STYLE_HERE] Exterior Abandoned or dilapidated structure. Should include:
- Cracked and crumbling wall segments
- Roof holes and collapsed beams
- Broken door and window frames
- Overgrown vegetation modules (vines, moss)
- Debris piles and rubble props
- Weathered textures and stains
Designed for post‐apocalyptic or fantasy ruin maps." },
            { "Ruined Building / Overgrown Ruin",
                @"[STYLE_HERE] Exterior Nature‐reclaimed ruin. Should include:
- Moss‐covered stone wall segments
- Tree root and vine overtaking modules
- Broken archways and toppled columns
- Fallen debris and leaf‐litter ground tiles
- Fungal growth and bioluminescent plant props
Designed for enchanted or post‐ruin settings." },

            // Tavern / Inn variants
            { "Tavern / Inn",
                @"[STYLE_HERE] Exterior A cozy tavern with lodging rooms. Should include:
- Common room floor tiles (wood planks, rugs)
- Bar counter and stools as separate modules
- Tables, chairs, barrels, and tankard props
- Fireplace or hearth element
- Upstairs room layouts with beds and chests
- Signboard and hanging lantern exterior pieces
Designed for top‐down or isometric perspective." },
            { "Tavern / Roadside Tavern",
                @"[STYLE_HERE] Exterior A rustic roadside tavern. Should include:
- Weathered timber walls and stone foundation
- Thatched roof or shingled variants
- Wooden signpost and lantern props
- Barrel seating and outdoor bench tiles
- Dirt path entryway and hitching post
Designed for trade road settings." },

            // Stable variants
            { "Stable / Stable",
                @"[STYLE_HERE] Exterior Equestrian shelter and tack room. Should include:
- Stall wall segments with gate variants
- Straw‐covered floor tiles
- Trough and bucket props
- Tack racks and saddle stands
- Roof modules (thatched or shingled)
- Exterior fence and hitching post pieces
Designed for top‐down or isometric perspective." },
            { "Stable / Coach House",
                @"[STYLE_HERE] Exterior Dedicated carriage storage and tack room. Should include:
- Broad double‐door modules
- Cobblestone floor segments
- Wheel rack and harness hooks
- Lanterns and bench props
- Lean‐to roof extensions
- Yard fence and hitching rail pieces
Designed for estate or inn settings." },

            // Mill variants
            { "Windmill / Watermill / Windmill / Watermill",
                @"[STYLE_HERE] Exterior Grain‐milling structure. Should include:
- Mill tower wall modules (stone or wood)
- Sails or water wheel pieces with rotation frames
- Millstone and grinding apron tiles
- Grain sack and barrel props
- Roof cap and axle elements
- Adjacent stream or field foundation tiles
Designed for top‐down or isometric perspective." },
            { "Windmill / Watermill / Hilltop Windmill",
                @"[STYLE_HERE] Exterior Scenic hilltop windmill. Should include:
- Weathered whitewashed walls
- Wide sail modules with wind animation variants
- Pathway and fence foundation tiles
- Small shed and grain sack props
- Grass slope and rock outcrop tiles
Designed for elevated rural maps." },

            // Barn / Granary variants
            { "Barn / Granary / Barn / Granary",
                @"[STYLE_HERE] Exterior Agricultural storage building. Should include:
- Timber or board‐and‐batten wall segments
- Loft floor and ramp modules
- Silo or bin sections
- Hay bale and crate props
- Large barn door and sliding track elements
- Surrounding fence and feeding trough pieces
Designed for top‐down or isometric perspective." },
            { "Barn / Granary / Dairy Barn",
                @"[STYLE_HERE] Exterior Barn specialized for livestock and milk. Should include:
- Sturdy plank walls and stone foundation
- Milking stall modules and trough tiles
- Milk pail and bucket props
- Ventilated roof panels
- Exterior hayloft door and ladder pieces
Designed for pastoral farm maps." },

            // Marketplace Stall variants
            { "Marketplace Stall / Marketplace Stall",
                @"[STYLE_HERE] Exterior Open‐air vendor stand. Should include:
- Canopy and frame modules
- Counter and display table tiles
- Crate, basket, and scale props
- Vendor signboard and hanging price tags
- Assorted produce or wares as separate mini‐tiles
- Stone or cobblestone ground segment
Designed for top‐down or isometric perspective." },
            { "Marketplace Stall / Spice Merchant Stall",
                @"[STYLE_HERE] Exterior Specialty spice vendor stand. Should include:
- Colorful awning and cloth drapes
- Jars, sacks, and mortar‐and‐pestle props
- Wooden scale and scoop modules
- Hanging lantern and signboard pieces
- Dusty ground overlay tiles
Designed for bustling bazaar scenes." },

            // Guild Hall variants
            { "Guild Hall / Guild Hall",
                @"[STYLE_HERE] Exterior Organization headquarters. Should include:
- Grand entrance façade modules with banners
- Large assembly room floor and wall tiles
- Council table, chairs, and podium props
- Vault door or safe niches
- Decorative crest and banner elements
- Staircase and balcony pieces
Designed for top‐down or isometric perspective." },
            { "Guild Hall / Artisan Guild Annex",
                @"[STYLE_HERE] Exterior Secondary workshop hall. Should include:
- Workshop benches and tool racks
- Open courtyard with forge or kiln modules
- Display cases for crafted goods
- Ornate crest and banner elements
- Cobblestone floor and archway tiles
Designed for craft district environments." },

            // Apothecary variants
            { "Apothecary / Alchemist Shop / Apothecary / Alchemist Shop",
                @"[STYLE_HERE] Exterior Potion‐brewing storefront. Should include:
- Wooden floor and shelving wall modules
- Glass cabinet and table tiles
- Potion bottle, herb bundle, mortar & pestle props
- Cauldron and burner elements
- Signboard with mortar‐and‐pestle icon
- Tiled sidewalk segment with potted plant
Designed for top‐down or isometric perspective." },
            { "Apothecary / Alchemist Shop / Herb Market Stall",
                @"[STYLE_HERE] Exterior Outdoor herbalist stall. Should include:
- Canopy modules with hanging herbs
- Wooden crates and burlap sack props
- Bundle and mortar‐and‐pestle elements
- Signboard with leaf icon
- Dirt ground and basket tiles
Designed for village market scenes." },

            // Tailor variants
            { "Tailor / Weaver / Tailor / Weaver",
                @"[STYLE_HERE] Exterior Garment‐making workshop. Should include:
- Wooden floor and partition wall modules
- Loom, sewing table, and mannequin props
- Fabric bolt racks and thread spool elements
- Scissors and measuring tape tiles
- Signboard with needle‐and‐thread icon
- Exterior awning and window display pieces
Designed for top‐down or isometric perspective." },
            { "Tailor / Weaver / Dye Works Shopfront",
                @"[STYLE_HERE] Exterior Dye‐work façade. Should include:
- Tiled stone floor and workshop wall modules
- Dye vats and hanging cloth props
- Barrel and funnel elements
- Signboard with dye icon
- Cracked ground tiles with spilled dye variants
Designed for artisan district maps." },

            // Butcher variants
            { "Butcher / Fishmonger / Butcher / Fishmonger",
                @"[STYLE_HERE] Exterior Meat and fish vendor stall. Should include:
- Stone or wood floor modules (with ice patches)
- Display counter tiles with hooks
- Meat cuts, whole fish, and scale props
- Cleaver and knife rack elements
- Hanging lantern and signboard pieces
- Drain grate or barrel props for run‐off
Designed for top‐down or isometric perspective." },
            { "Butcher / Fishmonger / Outdoor Meat Stall",
                @"[STYLE_HERE] Exterior Open‐air butcher’s display. Should include:
- Wooden slab counter modules
- Hanging meat and cleaver props
- Blood-stained floor tile variants
- Wooden crate and barrel elements
- Lantern and butcher’s signboard pieces
Designed for rural fair or market settings." },

            // Town Hall variants
            { "Town Hall / Courthouse / Town Hall / Courthouse",
                @"[STYLE_HERE] Exterior Civic administration building. Should include:
- Pillared façade and entrance steps modules
- Marble or stone floor tiles
- Judge’s bench, council seating, and podium props
- Banner and seal emblem elements
- Stained‐glass or large window frames
- Adjacent plaza or courtyard foundation tiles
Designed for top‐down or isometric perspective." },
            { "Town Hall / Courthouse / Magistrate’s Office",
                @"[STYLE_HERE] Exterior Smaller judicial admin building. Should include:
- Modest stone façade and doorway module
- Judgment bench and lectern props
- Scroll and seal icon elements
- Sidewalk segment with lamp post
Designed for small town settings." },

            // Guardhouse variants
            { "Guardhouse / Barracks / Guardhouse / Barracks",
                @"[STYLE_HERE] Exterior Military quarters and post. Should include:
- Stone or timber wall modules with arrow slits
- Bunk bed and footlocker props
- Armor rack and weapons stand elements
- Watch post roof or tower attachment
- Gate and portcullis pieces
- Exterior parade ground or training dummy tiles
Designed for top‐down or isometric perspective." },
            { "Guardhouse / Barracks / Frontier Outpost",
                @"[STYLE_HERE] Exterior Remote guard post with palisade. Should include:
- Rough-hewn wood wall segments
- Watchtower and signal fire props
- Sandbag barricade modules
- Tent or lean-to quarters
- Dirt path and footprint ground tiles
Designed for borderland maps." },

            // Church variants
            { "Church / Cathedral / Church / Cathedral",
                @"[STYLE_HERE] Exterior Religious worship structure. Should include:
- Stained‐glass window and buttress modules
- Nave floor tiles (stone or mosaic)
- Pews, altar, and lectern props
- Tower or spire roof segments
- Cross, bell, or lantern elements
- Cloister or courtyard foundation pieces
Designed for top‐down or isometric perspective." },
            { "Church / Cathedral / Chapel",
                @"[STYLE_HERE] Exterior Intimate worship pavilion. Should include:
- Simple stone wall and roof modules
- Small stained‐glass window variants
- Benches and altar props
- Lantern and cross sign elements
- Flower bed and path tiles
Designed for village or countryside settings." },

            // Library variants
            { "Library / Scriptorium / Library / Scriptorium",
                @"[STYLE_HERE] Exterior Knowledge repository. Should include:
- Tall bookshelf façade and wall panel modules
- Reading table, chair, and lectern props
- Scroll rack and book pile elements
- Stone or wooden floor tiles
- Stained‐glass or arched window frames
- Quiet courtyard or cloister foundation pieces
Designed for top‐down or isometric perspective." },
            { "Library / Scriptorium / Archive Entrance",
                @"[STYLE_HERE] Exterior Entry to rare‐books wing. Should include:
- Heavy wooden door and lintel modules
- Ornate stone archway and column props
- Lantern and signboard elements
- Mosaic floor and threshold tiles
Designed for grand academic settings." },

            // Watchtower variants
            { "Watchtower / Guard Tower / Watchtower / Guard Tower",
                @"[STYLE_HERE] Exterior Defensive lookout. Should include:
- Cylindrical or square stone wall segments
- Ladder or stair interior modules
- Battlement and arrow slit elements
- Roof platform and signal fire props
- Rope pulley and flagpole pieces
- Surrounding wall or palisade foundation tiles
Designed for top‐down or isometric perspective." },
            { "Watchtower / Guard Tower / Coastal Watch Post",
                @"[STYLE_HERE] Exterior Sentinel tower by the shoreline. Should include:
- Weathered stone walls with water stain modules
- Elevated platform and railing pieces
- Signal lantern and horn props
- Rocky base and wave‐splash ground tiles
Designed for maritime defense maps." },

            // Dock variants
            { "Dock / Warehouse / Dock / Warehouse",
                @"[STYLE_HERE] Exterior Waterfront loading area. Should include:
- Wooden plank dock and pier modules
- Warehouse wall and sliding door segments
- Crate, barrel, and rope coil props
- Mooring post and bollard elements
- Water edge overlay with wave tile variants
- Cargo net and pulley system pieces
Designed for top‐down or isometric perspective." },
            { "Dock / Warehouse / Fishing Wharf",
                @"[STYLE_HERE] Exterior Pier with fish crates and nets. Should include:
- Slatted wood deck and piling modules
- Ice block and crate props
- Fishing net and rope coil elements
- Lantern and buoy signboards
- Wet‐floor and splash effect tiles
Designed for coastal market scenes." },

            // School variants
            { "School / Academy",
                @"[STYLE_HERE] Exterior Educational institution. Should include:
- Classroom floor and chalkboard wall modules
- Desk, chair, and bookstack props
- Globe, scroll, and instrument elements
- Library alcove or lecture hall seating
- Banner or crest signage pieces
- Courtyard or quad foundation tiles
Designed for top‐down or isometric perspective." },
            { "School / Lecture Amphitheater",
                @"[STYLE_HERE] Exterior Open‐air tiered seating for classes. Should include:
- Stone bench modules in semicircle
- Lecture podium and chalkboard props
- Decorative archways and column pieces
- Banner and crest sign elements
- Pathway and grass quadrangle tiles
Designed for academic campus maps." },

            // Observatory variants
            { "Observatory / Observatory",
                @"[STYLE_HERE] Exterior Astronomical research tower. Should include:
- Circular base wall segments
- Dome roof with openable aperture modules
- Telescope, star‐chart table, and equipment props
- Gear and pulley elements for dome rotation
- Celestial motif decorations
- Adjacent platform or railing foundation pieces
Designed for top‐down or isometric perspective." },
            { "Observatory / Rooftop Observatory Deck",
                @"[STYLE_HERE] Exterior Open platform with telescope mount. Should include:
- Flat roof tiles with railing modules
- Telescope and tripod props
- Star map mural and chart elements
- Gear housing and bolt details
- Sky‐view aperture and lens cover pieces
Designed for urban science building settings." },

            // Shop variants
            { "Shop / Shop",
                @"[STYLE_HERE] Exterior Small retail storefront. Should include:
- Display window tiles with signage
- Modular shelving or counter pieces inside
- Door variants (open, closed)
- Exterior awning or canopy modules
- Signboard and lantern props
- Sidewalk segment with doormat or potted plants
Designed for top‐down or isometric perspective." },
            { "Shop / General Store",
                @"[STYLE_HERE] Exterior Countryside general store. Should include:
- Wide wooden façade and boardwalk modules
- Porch canopy and barrel props
- Signboard with store icon
- Sack and basket tiles with produce
- Bench and lamp post details
Designed for rural village maps." },

            // Blacksmith variants
            { "Blacksmith / Blacksmith",
                @"[STYLE_HERE] Exterior Forge and workshop building. Should include:
- Stone or brick wall segments with soot marks
- Open‐front forge hearth with glowing coals
- Anvil, workbench, and tool rack props
- Bellows and metal rod modules
- Roof vents or chimney stacks
- Exterior wood or stone floor tiles for yard
Designed for top‐down or isometric perspective." },
            { "Blacksmith / Armorer’s Forge",
                @"[STYLE_HERE] Exterior Specialized armor smith workshop. Should include:
- Reinforced stone walls and metal plating
- Large open-air smithy hearth
- Armor stand and rack props
- Shield and weapon mounting elements
- Chain and pulley roof attachments
Designed for military district scenes." },

            // Bakery variants
            { "Bakery / Bakery",
                @"[STYLE_HERE] Exterior Artisan bakery storefront. Should include:
- Large front window with display shelving
- Oven or brick‐built hearth tiles inside
- Counter and glass display case modules
- Bread racks, pastries, and sack props
- Exterior signboard and hanging lights
- Cobblestone or tiled sidewalk segment
Designed for top‐down or isometric perspective." },
            { "Bakery / Patisserie",
                @"[STYLE_HERE] Exterior Elegant patisserie façade. Should include:
- Ornate awning and window display modules
- Tiered cake and pastry stand props
- Marble counter and glass dome tiles
- Hanging signboard with pastry icon
- Flower box and table seating pieces
Designed for upscale market streets." },

            // Hospital variants
            { "Hospital / Hospital",
                @"[STYLE_HERE] Exterior Medical facility building. Should include:
- Clean wall segments (white or pastel) with signage
- Sliding door and window modules
- Reception desk and seating area pieces
- Medical equipment props: gurney, IV stand, cabinets
- Rooftop HVAC or helipad markings
- Sidewalk segment with ramp or handrail modules
Designed for top‐down or isometric perspective." },
            { "Hospital / Field Hospital",
                @"[STYLE_HERE] Exterior Temporary field clinic. Should include:
- Canvas tent wall and flap door modules
- Folding cot and medical crate props
- Lantern and supply trunk elements
- Makeshift walkway and sandbag tiles
- Triage signboard and flag markers
Designed for wartime or disaster relief maps." }
        }
},


        };


        private const string PREFS_KEY_TRASH_IMAGES = "TrashImages";

        #region File Operations 
        public static string SaveTextureToAsset(Texture2D texture, string basePath, string fileName)
        {
            // Ensure path ends with a slash
            if (!basePath.EndsWith("/"))
                basePath += "/";

            // Create directory if it doesn't exist
            if (!System.IO.Directory.Exists(basePath))
                System.IO.Directory.CreateDirectory(basePath);

            // Full path for the file
            string fullPath = basePath + fileName + ".png";

            // Convert to PNG and save
            byte[] pngData = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(fullPath, pngData);

            // Import the asset
            UnityEditor.AssetDatabase.ImportAsset(fullPath);
            UnityEditor.AssetDatabase.Refresh();

            return fullPath;
        }
 
        public static Texture2D LoadExternalTexture(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2);

            if (texture.LoadImage(fileData))
            {
                return texture;
            }

            // If loading failed, clean up and return null
            UnityEngine.Object.DestroyImmediate(texture);
            return null;
        } 
        public static Texture2D EnsureReadableTexture(Texture2D sourceTexture)
        {
            // If texture is already readable, return it
            if (sourceTexture.isReadable)
                return sourceTexture;

            // Create a temporary render texture
            RenderTexture tempRT = RenderTexture.GetTemporary(
                sourceTexture.width,
                sourceTexture.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);

            // Copy the source texture to the render texture
            Graphics.Blit(sourceTexture, tempRT);

            // Save the active render texture
            RenderTexture previousRT = RenderTexture.active;

            // Set the temporary render texture as active
            RenderTexture.active = tempRT;

            // Create a new readable texture
            Texture2D readableTexture = new Texture2D(
                sourceTexture.width,
                sourceTexture.height,
                TextureFormat.RGBA32,
                false);

            // Copy pixel data from the render texture
            readableTexture.ReadPixels(
                new Rect(0, 0, sourceTexture.width, sourceTexture.height),
                0, 0);

            readableTexture.Apply();

            // Restore the previous render texture
            RenderTexture.active = previousRT;

            // Release the temporary render texture
            RenderTexture.ReleaseTemporary(tempRT);

            return readableTexture;
        }
 
        public static string GetValidProjectSavePath(string requestedPath)
        {
            if (string.IsNullOrEmpty(requestedPath) || !requestedPath.StartsWith("Assets"))
            {
                return "Assets/AI Generated Images";
            }

            if (!Directory.Exists(requestedPath))
            {
                // Create the directory if it doesn't exist
                try
                {
                    Directory.CreateDirectory(requestedPath);
                    return requestedPath;
                }
                catch (System.Exception)
                {
                    // If we can't create the directory, fall back to default
                    return "Assets/AI Generated Images";
                }
            }

            return requestedPath;
        } 
        public static int GetTotalImageCount(string basePath)
        {
            if (!Directory.Exists(basePath))
                return 0;
                
            // Count all PNG files in the directory and subdirectories
            return Directory.GetFiles(basePath, "*.png", SearchOption.AllDirectories).Length;
        }
        
        public static int LoadImagesFromPath(
            string basePath, 
            ref Dictionary<DateTime, List<CreatedImageEntry>> createdImages, 
            string applicationDataPath,
            out int totalImagesInPath,
            int limit = 0,
            int offset = 0,
            List<string> trashImagePaths = null)
        {
            int loadedCount = 0;
            totalImagesInPath = 0;
            
            if (!Directory.Exists(basePath))
                return 0;
                
            // Find all PNG files in the directory and subdirectories
            string[] pngFiles = Directory.GetFiles(basePath, "*.png", SearchOption.AllDirectories);
            
            // Filter out trash images from the count
            if (trashImagePaths != null && trashImagePaths.Count > 0)
            {
                // Convert file paths to a consistent format for comparison
                var normalizedTrashPaths = trashImagePaths.Select(p => p.Replace('\\', '/')).ToHashSet();
                var nonTrashFiles = new List<string>();
                
                foreach (string file in pngFiles)
                {
                    string normalizedPath = file.Replace('\\', '/');
                    // If it's in the project, convert to asset path for comparison
                    if (normalizedPath.StartsWith(applicationDataPath))
                    {
                        string assetPath = "Assets" + normalizedPath.Substring(applicationDataPath.Length);
                        if (!normalizedTrashPaths.Contains(assetPath))
                        {
                            nonTrashFiles.Add(file);
                        }
                    }
                    // Otherwise use the full path
                    else if (!normalizedTrashPaths.Contains(normalizedPath))
                    {
                        nonTrashFiles.Add(file);
                    }
                }
                
                pngFiles = nonTrashFiles.ToArray();
            }
            
            // Get total count of non-trash images
            totalImagesInPath = pngFiles.Length;
            
            // If no images available, return
            if (totalImagesInPath == 0)
                return 0;
            
            // Sort files by creation date (newest first)
            pngFiles = pngFiles.OrderByDescending(file => File.GetCreationTime(file)).ToArray();
            
            // Apply pagination
            if (limit > 0 && offset < pngFiles.Length)
            {
                // Get subset of files based on limit and offset
                pngFiles = pngFiles.Skip(offset)
                                   .Take(limit)
                                   .ToArray();
            }
            else if (offset >= pngFiles.Length)
            {
                // No more images to load
                return 0;
            }

            foreach (string pngFile in pngFiles)
            {
                // Try to get creation time from file metadata
                DateTime creationDate = File.GetCreationTime(pngFile);

                // Load the texture
                Texture2D texture = null;

                // Check if the file is within the project
                string assetPath = pngFile.Replace('\\', '/');
                bool isInProject = assetPath.StartsWith(applicationDataPath);

                if (isInProject)
                {
                    // Convert to relative path for AssetDatabase
                    assetPath = "Assets" + assetPath.Substring(applicationDataPath.Length);
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                }
                else
                {
                    // External file - load manually
                    texture = LoadExternalTexture(pngFile);
                    assetPath = pngFile; // Use full path for external files
                }

                if (texture != null)
                {
                    // Try to fix texture import settings if it's in the project
                    if (isInProject)
                    {
                        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                        if (importer != null && (importer.textureType != TextureImporterType.Default ||
                                                importer.alphaIsTransparency != true ||
                                                importer.npotScale != TextureImporterNPOTScale.None))
                        {
                            importer.textureType = TextureImporterType.Default;
                            importer.alphaIsTransparency = true;
                            importer.npotScale = TextureImporterNPOTScale.None;
                            importer.SaveAndReimport();
                        }
                    }

                    string fileName = Path.GetFileName(pngFile);
                    var entry = new CreatedImageEntry
                    {
                        Texture = texture,
                        Prompt = EditorPrefs.GetString("uAI_"+fileName, "Unknown Prompt. Likely due to renaming."),
                        CreationDate = creationDate,
                        AssetPath = assetPath,
                        Model = ImageModel.GPTImage1, // Default model
                        IsInTrash = false
                    };

                    // Add to dictionary if not in trash
                    DateTime date = creationDate.Date;
                    if (createdImages.ContainsKey(date))
                    {
                        createdImages[date].Add(entry);
                    }
                    else
                    {
                        createdImages[date] = new List<CreatedImageEntry> { entry };
                    }
                    
                    loadedCount++;
                }
            }
            
            return loadedCount;
        }




 
        public static void ExportImage(CreatedImageEntry image, string applicationDataPath)
        {
            if (image == null || image.Texture == null) return;

            string defaultName = "GeneratedImage.png";
            string path = EditorUtility.SaveFilePanel("Export Image", "", defaultName, "png");

            if (!string.IsNullOrEmpty(path))
            {
                // Save the texture to the selected path
                byte[] bytes = image.Texture.EncodeToPNG();
                File.WriteAllBytes(path, bytes);

                // If saved within the project, refresh AssetDatabase
                if (path.StartsWith(applicationDataPath))
                {
                    string relativePath = "Assets" + path.Substring(applicationDataPath.Length);
                    AssetDatabase.ImportAsset(relativePath);
                }

                EditorUtility.DisplayDialog("Export Successful", "Image exported successfully.", "OK");
            }
        }
        #endregion

        #region Settings Management 
        public static void LoadSettings(
            string prefsKeyPrefix,
            ref string savePath,
            ref string externalSavePath,
            ref bool useExternalPath,
            ref ImageModel defaultModel,
            ref Size defaultSize,
            ref Quality defaultQuality,
            ref int defaultImageCount)
        {
            // Load settings from EditorPrefs with defaults
            savePath = EditorPrefs.GetString(prefsKeyPrefix + "SavePath", "Assets/AI Generated Images");
            externalSavePath = EditorPrefs.GetString(prefsKeyPrefix + "ExternalPath", "");
            useExternalPath = EditorPrefs.GetBool(prefsKeyPrefix + "UseExternalPath", false);
            defaultModel = (ImageModel)EditorPrefs.GetInt(prefsKeyPrefix + "DefaultModel", (int)ImageModel.GPTImage1);
            defaultSize = (Size)EditorPrefs.GetInt(prefsKeyPrefix + "DefaultSize", (int)Size.Size1024x1024);
            defaultQuality = (Quality)EditorPrefs.GetInt(prefsKeyPrefix + "DefaultQuality", (int)Quality.high);
            defaultImageCount = EditorPrefs.GetInt(prefsKeyPrefix + "DefaultCount", 1);
        } 
        public static void SaveSettings(
            string prefsKeyPrefix,
            string savePath,
            string externalSavePath,
            bool useExternalPath,
            ImageModel defaultModel,
            Size defaultSize,
            Quality defaultQuality,
            int defaultImageCount)
        {
            // Save all settings to EditorPrefs
            EditorPrefs.SetString(prefsKeyPrefix + "SavePath", savePath);
            EditorPrefs.SetString(prefsKeyPrefix + "ExternalPath", externalSavePath);
            EditorPrefs.SetBool(prefsKeyPrefix + "UseExternalPath", useExternalPath);
            EditorPrefs.SetInt(prefsKeyPrefix + "DefaultModel", (int)defaultModel);
            EditorPrefs.SetInt(prefsKeyPrefix + "DefaultSize", (int)defaultSize);
            EditorPrefs.SetInt(prefsKeyPrefix + "DefaultQuality", (int)defaultQuality);
            EditorPrefs.SetInt(prefsKeyPrefix + "DefaultCount", defaultImageCount);
        }
        
        public static List<string> GetTrashImagePaths(string prefsKeyPrefix)
        {
            List<string> trashPaths = new List<string>();
            
            // Get JSON from EditorPrefs
            string json = EditorPrefs.GetString(prefsKeyPrefix + PREFS_KEY_TRASH_IMAGES, "");
            
            if (!string.IsNullOrEmpty(json))
            {
                // Deserialize
                TrashImagesData data = JsonUtility.FromJson<TrashImagesData>(json);
                
                if (data != null && data.TrashPaths != null)
                {
                    trashPaths = data.TrashPaths;
                }
            }
            
            return trashPaths;
        }
        public static List<CreatedImageEntry> LoadTrashImages(
            string prefsKeyPrefix,
            ref Dictionary<DateTime, List<CreatedImageEntry>> createdImages)
        {
            List<CreatedImageEntry> trashImages = new List<CreatedImageEntry>();

            // Get JSON from EditorPrefs
            string json = EditorPrefs.GetString(prefsKeyPrefix + PREFS_KEY_TRASH_IMAGES, "");

            if (string.IsNullOrEmpty(json))
                return trashImages;

            // Deserialize
            TrashImagesData data = JsonUtility.FromJson<TrashImagesData>(json);

            if (data == null || data.TrashPaths == null)
                return trashImages;

            // Process each path
            foreach (string path in data.TrashPaths)
            {
                // Try to find the image in created images
                CreatedImageEntry foundImage = null;

                foreach (var date in createdImages.Keys.ToList())
                {
                    foreach (var image in createdImages[date].ToList())
                    {
                        if (image.AssetPath == path)
                        {
                            foundImage = image;
                            createdImages[date].Remove(image);

                            // If date group is now empty, remove it
                            if (createdImages[date].Count == 0)
                            {
                                createdImages.Remove(date);
                            }

                            break;
                        }
                    }

                    if (foundImage != null)
                        break;
                }

                // If not found in created images, try to load directly
                if (foundImage == null)
                {
                    Texture2D texture = null;

                    // Check if path is an asset path or external path
                    if (path.StartsWith("Assets/"))
                    {
                        texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    }
                    else if (File.Exists(path))
                    {
                        // External file
                        texture = LoadExternalTexture(path);
                    }

                    if (texture != null)
                    {
                        foundImage = new CreatedImageEntry
                        {
                            Texture = texture,
                            Prompt = "Unknown Prompt",
                            CreationDate = File.GetCreationTime(path),
                            AssetPath = path,
                            Model = ImageModel.GPTImage1,
                            IsInTrash = true
                        };
                    }
                }

                // Add to trash if found
                if (foundImage != null)
                {
                    foundImage.IsInTrash = true;
                    trashImages.Add(foundImage);
                }
            }

            return trashImages;
        }

        public static void SaveTrashImages(string prefsKeyPrefix, List<CreatedImageEntry> trashImages)
        {
            // Create a list of asset paths for trash images
            List<string> trashPaths = new List<string>();
            foreach (var image in trashImages)
            {
                if (!string.IsNullOrEmpty(image.AssetPath))
                {
                    trashPaths.Add(image.AssetPath);
                }
            }

            // Serialize to JSON
            string json = JsonUtility.ToJson(new TrashImagesData { TrashPaths = trashPaths });

            // Save to EditorPrefs
            EditorPrefs.SetString(prefsKeyPrefix + PREFS_KEY_TRASH_IMAGES, json);
        }
        
        public static void RestoreImageFromTrash(
            CreatedImageEntry image,
            ref List<CreatedImageEntry> trashImages,
            ref Dictionary<DateTime, List<CreatedImageEntry>> createdImages)
        {
            // Remove from trash
            trashImages.Remove(image);

            // Mark as not in trash
            image.IsInTrash = false;

            // Add back to created images
            DateTime date = image.CreationDate.Date;
            if (createdImages.ContainsKey(date))
            {
                createdImages[date].Add(image);
            }
            else
            {
                createdImages[date] = new List<CreatedImageEntry> { image };
            }
        }
        #endregion

        #region Mask Drawing
        
        public static Texture2D CreateBlankMask(int width, int height)
        {
            Texture2D mask = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // Fill with transparent pixels
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            mask.SetPixels(pixels);
            mask.Apply();

            return mask;
        }
        
        public static Texture2D CreateBrushTexture(int size, bool isEraser)
        {
            Texture2D brush = new Texture2D(size, size, TextureFormat.RGBA32, false);

            // Calculate center
            float center = size / 2f;
            float maxDist = center;

            // Create pixels
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Calculate distance from center
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Normalize distance (0-1)
                    float normalizedDist = dist / maxDist;

                    // Calculate alpha (1 at center, 0 at edge)
                    float alpha = Mathf.Clamp01(1f - normalizedDist);

                    // Smooth falloff
                    alpha = Mathf.SmoothStep(0, 1, alpha);

                    // Set color
                    if (isEraser)
                    {
                        // Eraser: black with alpha
                        pixels[y * size + x] = new Color(0, 0, 0, alpha);
                    }
                    else
                    {
                        // Brush: white with alpha
                        pixels[y * size + x] = new Color(1, 1, 1, alpha);
                    }
                }
            }

            brush.SetPixels(pixels);
            brush.Apply();

            return brush;
        }


        public static Texture2D ProcessMaskForAPI(Texture2D maskTexture)
        {
            // Create a copy to process
            Texture2D processedMask = new Texture2D(
                maskTexture.width,
                maskTexture.height,
                TextureFormat.RGBA32,
                false);

            Color[] pixels = maskTexture.GetPixels();
            Color[] processedPixels = new Color[pixels.Length];

            // Process pixels: white = areas to edit, black = areas to keep
            for (int i = 0; i < pixels.Length; i++)
            {
                // Convert to black and white based on alpha
                if (pixels[i].a > 0.1f)
                {
                    // Areas with any alpha should be edited (white)
                    processedPixels[i] = Color.white;
                }
                else
                {
                    // Clear areas should be preserved (black)
                    processedPixels[i] = Color.black;
                }
            }

            processedMask.SetPixels(processedPixels);
            processedMask.Apply();

            return processedMask;
        }
        
        public static void HandleMaskDrawingGUI(
            CreatedImageEntry imageBeingEdited,
            ref Texture2D maskTexture,
            float brushSize,
            bool isErasing,
            ref Vector2 lastDrawPosition,
            ref bool maskChanged,
            EditorWindow window)
        {
            if (imageBeingEdited == null || imageBeingEdited.Texture == null || maskTexture == null)
                return;

            // Calculate drawing area size while maintaining aspect ratio
            float width = imageBeingEdited.Texture.width;
            float height = imageBeingEdited.Texture.height;
            float ratio = width / height;

            float displayWidth, displayHeight;

            // Get the available space
            Rect availableRect = GUILayoutUtility.GetRect(10, 1000, 10, 512);

            if (ratio >= 1) // Wider than tall
            {
                displayWidth = Mathf.Min(512, availableRect.width);
                displayHeight = displayWidth / ratio;
            }
            else // Taller than wide
            {
                displayHeight = Mathf.Min(512, availableRect.height);
                displayWidth = displayHeight * ratio;
            }

            // Center the drawing area
            float x = (availableRect.width - displayWidth) * 0.5f;
            float y = (availableRect.height - displayHeight) * 0.5f;

            Rect drawRect = new Rect(x, y, displayWidth, displayHeight);

            // Draw the source image
            GUI.DrawTexture(drawRect, imageBeingEdited.Texture);

            // Draw the mask over it with semi-transparency
            Color oldColor = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.5f);
            GUI.DrawTexture(drawRect, maskTexture);
            GUI.color = oldColor;

            // Handle mouse input for drawing
            Event e = Event.current;
            Vector2 mousePos = e.mousePosition;

            if (drawRect.Contains(mousePos))
            {
                // Show the cursor as a circle when hovering over the image
                Handles.color = isErasing ? Color.black : Color.white;
                Handles.DrawWireDisc(mousePos, Vector3.forward, brushSize * 0.5f);

                // Handle mouse click/drag
                if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                {
                    // Convert mouse position to texture coordinates
                    Vector2 texCoord = new Vector2(
                        (mousePos.x - drawRect.x) / drawRect.width,
                        1.0f - (mousePos.y - drawRect.y) / drawRect.height);

                    int x_pos = Mathf.RoundToInt(texCoord.x * maskTexture.width);
                    int y_pos = Mathf.RoundToInt(texCoord.y * maskTexture.height);

                    // Draw a brush stroke
                    DrawBrushStroke(maskTexture, x_pos, y_pos, brushSize, isErasing, ref lastDrawPosition);
                    maskChanged = true;

                    // Force repaint
                    e.Use();
                }

                // Ensure continuous painting
                if (e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
                {
                    window.Repaint();
                }
            }

            // Reset last position on mouse up
            if (e.type == EventType.MouseUp)
            {
                lastDrawPosition = Vector2.zero;
            }
        }
        
        private static void DrawBrushStroke(Texture2D maskTexture, int x, int y, float brushSize, bool isErasing, ref Vector2 lastDrawPosition)
        {
            // If this is the first point of a stroke, just draw a brush at the current position
            if (lastDrawPosition == Vector2.zero)
            {
                DrawBrushAt(maskTexture, x, y, brushSize, isErasing);
            }
            else
            {
                // Interpolate between last and current position for smooth stroke
                Vector2 start = lastDrawPosition;
                Vector2 end = new Vector2(x, y);
                float distance = Vector2.Distance(start, end);
                int steps = Mathf.Max(1, Mathf.FloorToInt(distance));

                for (int i = 0; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    Vector2 pos = Vector2.Lerp(start, end, t);
                    DrawBrushAt(maskTexture, Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), brushSize, isErasing);
                }
            }

            // Update last position
            lastDrawPosition = new Vector2(x, y);
        }


        private static void DrawBrushAt(Texture2D maskTexture, int centerX, int centerY, float brushSize, bool isErasing)
        {
            int radius = Mathf.FloorToInt(brushSize * 0.5f);
            Color color = isErasing ? Color.clear : new Color(1, 1, 1, 0.5f); // White with 50% alpha for drawing

            // Draw a circle centered at (centerX, centerY)
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    // Check if point is within radius
                    if (x * x + y * y <= radius * radius)
                    {
                        int pixelX = centerX + x;
                        int pixelY = centerY + y;

                        // Check bounds
                        if (pixelX >= 0 && pixelX < maskTexture.width &&
                            pixelY >= 0 && pixelY < maskTexture.height)
                        {
                            maskTexture.SetPixel(pixelX, pixelY, color);
                        }
                    }
                }
            }

            // Apply changes to the texture
            maskTexture.Apply();
        }
        #endregion

        #region Image Generation

        public static void GenerateImages(
            string prompt,
            ImageModel model,
            Size size,
            Quality quality,
            int count,
            List<Texture2D> referenceImages,
            System.Action<List<CreatedImageEntry>, bool> callback,
            string savePath,
            string externalSavePath,
            bool useExternalPath,
            string applicationDataPath)
        {
            // Clean up reference images list (remove null entries)
            List<Texture2D> validReferenceImages = new List<Texture2D>();
            foreach (var img in referenceImages)
            {
                if (img != null)
                {
                    // Ensure texture is readable
                    validReferenceImages.Add(EnsureReadableTexture(img));
                }
            }

            // Create the request based on whether we have reference images
            if (validReferenceImages.Count > 0)
            {
                // Use multiple reference images
                var request = new DALLEClientExtension.MultiImageRequest
                {
                    Prompt = prompt,
                    Model = model,
                    Count = count,
                    Size = size,
                    Quality = quality,
                    ReferenceImages = validReferenceImages
                };

                // Register callback
                DALLEClient.Instance.OnResponseReceived = (images) =>
                {
                    ProcessGeneratedImages(images, prompt, model, callback, savePath, externalSavePath, useExternalPath, applicationDataPath);
                };

                // Generate with references
                DALLEClient.Instance.GenerateWithReferences(request);
            }
            else
            {
                // Standard image generation
                var request = new ImageGenerationRequest
                {
                    Prompt = prompt,
                    Model = model,
                    Count = count,
                    Size = size,
                    Quality = quality
                };

                // Register callback
                DALLEClient.Instance.OnResponseReceived = (images) =>
                {
                    ProcessGeneratedImages(images, prompt, model, callback, savePath, externalSavePath, useExternalPath, applicationDataPath);
                };

                // Generate image
                DALLEClient.Instance.GenerateImage(request);
            }
        }

        private static void ProcessGeneratedImages(
            List<DALLEImageResult> images,
            string prompt,
            ImageModel model,
            System.Action<List<CreatedImageEntry>, bool> callback,
            string savePath,
            string externalSavePath,
            bool useExternalPath,
            string applicationDataPath)
        {
            // Check if we got any images
            if (images == null || images.Count == 0)
            {
                callback?.Invoke(null, false);
                return;
            }

            // Save the generated images
            DateTime now = DateTime.Now;
            var createdEntries = new List<CreatedImageEntry>();

            foreach (var image in images)
            {
                var entry = new CreatedImageEntry
                {
                    Texture = image.Texture,
                    Prompt = prompt,
                    RevisedPrompt = image.RevisedPrompt,
                    CreationDate = now,
                    Model = model,
                    IsInTrash = false
                };
                createdEntries.Add(entry);
            }

            // Save images to disk
            SaveImagesToStorage(createdEntries, savePath, externalSavePath, useExternalPath, applicationDataPath);

            // Invoke callback
            callback?.Invoke(createdEntries, true);
        }

        /// <summary>
        /// Generate an edited image with a mask
        /// </summary>
        public static void GenerateEditedImage(
            CreatedImageEntry imageToEdit,
            Texture2D maskTexture,
            string prompt,
            System.Action<CreatedImageEntry, bool> callback,
            string savePath,
            string externalSavePath,
            bool useExternalPath,
            string applicationDataPath)
        {
            // Process the mask for DALL-E API
            Texture2D processedMask = ProcessMaskForAPI(maskTexture);

            // Create edit request
            var request = new ImageEditRequest
            {
                Image = imageToEdit.Texture,
                Mask = processedMask,
                Prompt = prompt,
                Model = ImageModel.GPTImage1, // Using GPT Image for edits
                Count = 1
            };

            // Register callback
            DALLEClient.Instance.OnResponseReceived = (images) =>
            {
                // Clean up the processed mask
                if (processedMask != null)
                {
                    UnityEngine.Object.DestroyImmediate(processedMask);
                }

                // Check if we got any images
                if (images == null || images.Count == 0)
                {
                    callback?.Invoke(null, false);
                    return;
                }

                // Create entry for the edited image
                DateTime now = DateTime.Now;
                var editedEntry = new CreatedImageEntry
                {
                    Texture = images[0].Texture,
                    Prompt = prompt,
                    RevisedPrompt = images[0].RevisedPrompt,
                    CreationDate = now,
                    Model = ImageModel.GPTImage1,
                    IsInTrash = false
                };

                // Save the edited image
                SaveImagesToStorage(new List<CreatedImageEntry> { editedEntry }, savePath, externalSavePath, useExternalPath, applicationDataPath);

                // Invoke callback
                callback?.Invoke(editedEntry, true);
            };

            // Send request
            DALLEClient.Instance.EditImage(request);
        }

        /// <summary>
        /// Save generated images to storage
        /// </summary>
        private static void SaveImagesToStorage(
            List<CreatedImageEntry> entries,
            string savePath,
            string externalSavePath,
            bool useExternalPath,
            string applicationDataPath)
        {
            // Get the save path (either project or external)
            string basePath = useExternalPath ? externalSavePath : savePath;

            // Create directory if it doesn't exist
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            // Get the current date for the filename
            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");

            // Save each image
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                string fileName = $"generated_{currentDate}_{i + 1}.png";
                string fullPath = Path.Combine(basePath, fileName);

                // Make sure we don't overwrite existing files
                int suffix = 1;
                while (File.Exists(fullPath))
                {
                    fileName = $"generated_{currentDate}_{i + 1}_{suffix}.png";
                    fullPath = Path.Combine(basePath, fileName);
                    suffix++;
                }

                // Convert to PNG
                byte[] bytes = entry.Texture.EncodeToPNG();
                File.WriteAllBytes(fullPath, bytes);

                // Update the entry with the asset path
                entry.AssetPath = fullPath.Replace('\\', '/');

                // Import the asset if it's in the project
                if (!useExternalPath)
                {
                    // Make path relative to project if needed
                    string assetPath = entry.AssetPath;
                    if (assetPath.StartsWith(applicationDataPath))
                    {
                        assetPath = "Assets" + assetPath.Substring(applicationDataPath.Length);
                        entry.AssetPath = assetPath;
                    }

                    AssetDatabase.ImportAsset(assetPath);

                    // Fix texture import settings to preserve aspect ratio
                    TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer != null)
                    {
                        importer.textureType = TextureImporterType.Default;
                        importer.alphaIsTransparency = true;
                        importer.npotScale = TextureImporterNPOTScale.None;
                        importer.SaveAndReimport();
                    }
                }


                //make EditorPref save the prompt
                EditorPrefs.SetString("uAI_"+fileName, entry.Prompt);
            }

            // Refresh the AssetDatabase if we're saving to the project
            if (!useExternalPath)
            {
                AssetDatabase.Refresh();
            }
        }
        #endregion

        #region UI Helpers
        /// <summary>
        /// Populate the creations panel with images
        /// </summary>
        public static void PopulateCreationsPanel(
            ScrollView scrollView,
            Dictionary<DateTime, List<CreatedImageEntry>> createdImages,
            System.Action<CreatedImageEntry> onImageClicked,
            System.Action<ContextClickEvent, CreatedImageEntry> onContextMenu,
            float thumbnailSize = 100f,
            System.Action onLoadMoreClicked = null,
            bool hasMoreImages = false)
        {
            // Sort dates from newest to oldest
            var sortedDates = createdImages.Keys.OrderByDescending(date => date);

            if (!sortedDates.Any())
            {
                // Show empty state
                var emptyLabel = new Label("No images created yet. Use the Create panel to generate new images.");
                emptyLabel.style.marginTop = 20;
                emptyLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                scrollView.Add(emptyLabel);
                return;
            }

            // Create date groups
            foreach (var date in sortedDates)
            {
                var dateGroup = CreateDateGroup(date);
                scrollView.Add(dateGroup);

                var images = createdImages[date];

                // Add thumbnails grid for this date
                var thumbnailsGrid = new VisualElement();
                thumbnailsGrid.style.flexDirection = FlexDirection.Row;
                thumbnailsGrid.style.flexWrap = Wrap.Wrap;
                dateGroup.Add(thumbnailsGrid);

                // Add thumbnails
                foreach (var image in images)
                {
                    var thumbnail = CreateThumbnail(image, onImageClicked, onContextMenu, thumbnailSize);
                    thumbnailsGrid.Add(thumbnail);
                }
            }
            
            // Add "Load More" button if there are more images to load
            if (hasMoreImages && onLoadMoreClicked != null)
            {
                var loadMoreButton = new Button(onLoadMoreClicked);
                loadMoreButton.text = "Load More Images";
                loadMoreButton.name = "load-more-button";
                loadMoreButton.AddToClassList("load-more-button");
                loadMoreButton.style.alignSelf = Align.Center;
                loadMoreButton.style.marginTop = 10;
                loadMoreButton.style.marginBottom = 20;
                scrollView.Add(loadMoreButton);
            }
        }


        /// <summary>
        /// Create a date group visual element
        /// </summary>
        private static VisualElement CreateDateGroup(DateTime date)
        {
            var group = new VisualElement();
            group.AddToClassList("date-group");

            var dateLabel = new Label(date.ToString("dddd, MMMM d, yyyy"));
            dateLabel.style.fontSize = 14;
            dateLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            group.Add(dateLabel);

            return group;
        }

        /// <summary>
        /// Create a thumbnail visual element for an image
        /// </summary>
        private static VisualElement CreateThumbnail(
            CreatedImageEntry image,
            System.Action<CreatedImageEntry> onImageClicked,
            System.Action<ContextClickEvent, CreatedImageEntry> onContextMenu,
            float size = 100f)
        {
            var thumbnail = new VisualElement();
            thumbnail.AddToClassList("image-thumbnail");
            thumbnail.style.width = size;
            thumbnail.style.height = size;
            thumbnail.style.marginRight = 10;
            thumbnail.style.marginBottom = 10;
            thumbnail.style.overflow = Overflow.Hidden;

            // Background
            thumbnail.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);

            // Image - Fix aspect ratio with ScaleToFit
            var imageElement = new Image();
            imageElement.image = image.Texture;
            imageElement.scaleMode = ScaleMode.ScaleToFit;
            imageElement.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            imageElement.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            imageElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            imageElement.style.alignSelf = Align.Center; // Center image
            thumbnail.Add(imageElement);

            // Click handler
            thumbnail.RegisterCallback<ClickEvent>(evt =>
            {
                onImageClicked?.Invoke(image);
            });

            // Context menu
            thumbnail.RegisterCallback<ContextClickEvent>(evt =>
            {
                onContextMenu?.Invoke(evt, image);
                evt.StopPropagation();
            });

            return thumbnail;
        }

        /// <summary>
        /// Populate the trash panel with images
        /// </summary>
        public static void PopulateTrashPanel(
            ScrollView scrollView,
            List<CreatedImageEntry> trashImages,
            System.Action<ContextClickEvent, CreatedImageEntry> onContextMenu)
        {
            if (trashImages.Count == 0)
            {
                // Show empty state
                var emptyLabel = new Label("Trash is empty.");
                emptyLabel.style.marginTop = 20;
                emptyLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                scrollView.Add(emptyLabel);
                return;
            }

            // Group by date
            var groupedImages = trashImages
                .GroupBy(img => img.CreationDate.Date)
                .OrderByDescending(group => group.Key);

            // Create date groups
            foreach (var group in groupedImages)
            {
                var dateGroup = CreateDateGroup(group.Key);
                scrollView.Add(dateGroup);

                // Add thumbnails grid for this date
                var thumbnailsGrid = new VisualElement();
                thumbnailsGrid.style.flexDirection = FlexDirection.Row;
                thumbnailsGrid.style.flexWrap = Wrap.Wrap;
                dateGroup.Add(thumbnailsGrid);

                // Add thumbnails
                foreach (var image in group)
                {
                    var thumbnail = CreateTrashThumbnail(image, onContextMenu);
                    thumbnailsGrid.Add(thumbnail);
                }
            }
        }

        /// <summary>
        /// Create a thumbnail for a trash image
        /// </summary>
        private static VisualElement CreateTrashThumbnail(
            CreatedImageEntry image,
            System.Action<ContextClickEvent, CreatedImageEntry> onContextMenu)
        {
            var thumbnail = new VisualElement();
            thumbnail.AddToClassList("image-thumbnail");
            thumbnail.style.width = 100;
            thumbnail.style.height = 100;
            thumbnail.style.marginRight = 10;
            thumbnail.style.marginBottom = 10;
            thumbnail.style.overflow = Overflow.Hidden;

            // Background
            thumbnail.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);

            // Image (slightly dimmed)
            var imageElement = new Image();
            imageElement.image = image.Texture;
            imageElement.scaleMode = ScaleMode.ScaleToFit;
            imageElement.style.width = 100;
            imageElement.style.height = 100;
            imageElement.style.opacity = 0.7f;
            thumbnail.Add(imageElement);

            // Context menu
            thumbnail.RegisterCallback<ContextClickEvent>(evt =>
            {
                onContextMenu?.Invoke(evt, image);
                evt.StopPropagation();
            });

            return thumbnail;
        }
        #endregion
    }
}