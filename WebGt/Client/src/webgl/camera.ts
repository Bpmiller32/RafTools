/* -------------------------------------------------------------------------- */
/*             The camera and camera controls for the webgl scene             */
/* -------------------------------------------------------------------------- */

import * as THREE from "three";
import Experience from "./experience";
import Sizes from "./utils/sizes";
import Debug from "./utils/debug";
import Input from "./utils/input";
import Time from "./utils/time";

export default class Camera {
  private experience: Experience;
  private sizes: Sizes;
  private scene: THREE.Scene;
  private input: Input;
  private time: Time;

  private debug!: Debug;

  public instance!: THREE.PerspectiveCamera;

  constructor() {
    this.experience = Experience.getInstance();
    this.sizes = this.experience.sizes;
    this.scene = this.experience.scene;
    this.time = this.experience.time;
    this.input = this.experience.input;

    this.setInstance();

    // Debug GUI
    if (this.experience.debug.isActive) {
      this.debug = this.experience.debug;

      const cameraDebug = this.debug.ui?.addFolder("cameraDebug");
      cameraDebug?.open();
      cameraDebug?.add(this.instance.position, "x").name("xPosition").listen();
      cameraDebug?.add(this.instance.position, "y").name("yPosition").listen();
      cameraDebug?.add(this.instance.position, "z").name("zPosition").listen();
    }
  }

  private setInstance() {
    this.instance = new THREE.PerspectiveCamera(
      35,
      this.sizes.width / this.sizes.height,
      0.1,
      500
    );

    this.scene.add(this.instance);

    // Set initial camera position
    this.instance.position.z = 10;
    // Initialize targetPosition to the same initial position as the camera
    this.input.cameraTargetPosition.z = 10;
  }

  public resize() {
    this.instance.aspect = this.sizes.width / this.sizes.height;
    this.instance.updateProjectionMatrix();
  }

  public update() {
    this.instance.position.lerp(
      this.input.cameraTargetPosition,
      this.input.interpolationSmoothness * this.time.delta * 60
    );
  }
}
