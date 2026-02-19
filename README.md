# Mille Bornes - Blazor Edition 🏎️💨

A full-featured, standalone web implementation of the classic 1954 French card game, built entirely with **Blazor WebAssembly**.

## 🎮 Features
- **AI Opponent:** A priority-based AI that knows when to sabotage you and when to race for the finish line.
- **Dynamic Race Track:** A real-time visual progress tracker with animated SVG racing cars.
- **Rules Engine:** Strict adherence to the official Collector's Edition rules, including:
  - **Coup Fourré:** Instant interrupts and turn-stealing.
  - **Safeties:** Permanent immunities and immediate hazard clearing.
  - **Complex Scoring:** Detailed breakdown including Safe Trip, Shutout, and Delayed Action bonuses.
- **Narrative Game Log:** A reactive sidebar keeping track of every move and logic event.
- **Unit Tested:** Core game rules and scoring math are locked in with an xUnit test suite.

## 🛠️ Tech Stack
- **Frontend:** Blazor WebAssembly (.NET 8.0)
- **Architecture:** Component-based UI with a decoupled Service-based Game Engine.
- **Styling:** Custom Scoped CSS with Flexbox/Grid and 3D card stack effects.
- **CI/CD:** Automated deployment via GitHub Actions.

## 🚀 How to Play
1. Draw a card.
2. Play a **Roll!** card to start your engine.
3. Accumulate exactly **1000km** in distance cards.
4. Avoid Hazards (Flat Tire, Accident, etc.) and use Remedies to get back in the race.
5. Reach 5,000 total points across multiple rounds to become the Grand Champion!

---
*Created as a deep-dive into Blazor state management and system architecture.*