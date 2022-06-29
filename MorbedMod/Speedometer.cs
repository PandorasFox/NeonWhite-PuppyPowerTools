using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using MelonLoader;
using UnityEngine;

namespace MorbedMod {
    public class Speedometer : MelonMod {
        public bool is_dashing = false;
        public Vector3 total_velocity = Vector3.zero;

        public float lateral_velocity_magnitude = 0f;
        public float vertical_velocity = 0f;

        public Vector3 pos = Vector3.zero;
        public Vector3 dir = Vector3.zero;

        // TODO: add preferences in OnApplicationStart for speedometer

        public override void OnLateUpdate() {
            if (RM.mechController && RM.mechController.GetIsAlive()) {
                this.pos = RM.playerPosition;
                this.dir = RM.mechController.playerCamera.PlayerCam.transform.forward;

                Vector3 normal_velocity = RM.drifter.Velocity;
                Vector3 move_velocity = RM.drifter.MovementVelocity;

                this.total_velocity = normal_velocity + move_velocity;

                this.is_dashing = RM.drifter.GetIsDashing();

                this.vertical_velocity = this.total_velocity.y;

                this.lateral_velocity_magnitude = Mathf.Sqrt(
                    Mathf.Pow(total_velocity.x, 2f) + Mathf.Pow(total_velocity.z, 2f)
                );
            }
        }

        public string Vec3ToString(Vector3 v) {
            return v.x.ToString("N2") + ", " + v.y.ToString("N2") + ", " + v.z.ToString("N2");
        }

        public static GUIStyle SpeedometerStyle() {
            GUIStyle style = new GUIStyle();

            style.fixedHeight = 30;
            style.fontSize = 20;
            style.normal.textColor = Color.yellow;

            return style;
        }

        public void DrawText(int x_offset, int y_offset, string s) {
            GUIStyle style = SpeedometerStyle();

            GUIStyle outline_style = SpeedometerStyle();
            outline_style.normal.textColor = Color.black;
            int outline_strength = 2;

            Rect r = new Rect(x_offset, y_offset, 120, 30);

            for (int i = -outline_strength; i <= outline_strength; ++i) {
                GUI.Label(new Rect(r.x - outline_strength, r.y + i, r.width, r.height), s, outline_style);
                GUI.Label(new Rect(r.x + outline_strength, r.y + i, r.width, r.height), s, outline_style);
            }
            for (int i = -outline_strength + 1; i <= outline_strength - 1; ++i) {
                GUI.Label(new Rect(r.x + i, r.y - outline_strength, r.width, r.height), s, outline_style);
                GUI.Label(new Rect(r.x + i, r.y + outline_strength, r.width, r.height), s, outline_style);
            }
            GUI.Label(r, s, SpeedometerStyle());
        }

        public override void OnGUI() {
            if (RM.mechController && RM.mechController.GetIsAlive()) {
                int x_offset = 30;
                int y_offset = 30;

                GUIStyle style = SpeedometerStyle();
                // draw position
                DrawText(x_offset, y_offset, Vec3ToString(this.pos) + " | " + Vec3ToString(this.dir));
                y_offset += 31;

                // draw Velocities
                DrawText(x_offset, y_offset, "Norm Velocity: " + this.lateral_velocity_magnitude.ToString("N2") + "  ( " + Vec3ToString(this.total_velocity) + " )");
                y_offset += 31;
                DrawText(x_offset + 5, y_offset, "y: " + this.vertical_velocity.ToString("N2"));
                y_offset += 31;

                if (this.is_dashing) {
                    DrawText(x_offset, y_offset, "Dashing!");
                    y_offset += 31;
                }
            }
        }
    }
}
