/* -------------------------------------------------------------------------- */
/*             The camera and camera controls for the webgl scene             */
/* -------------------------------------------------------------------------- */

import * as THREE from "three";
import Experience from "./experience";
import Sizes from "./utils/sizes";
import debugCamera from "./utils/debug/debugCamera";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";

export default class Camera {
  private experience: Experience;
  private sizes: Sizes;
  private scene: THREE.Scene;

  public instance!: THREE.PerspectiveCamera;
  private controls!: OrbitControls;

  constructor() {
    this.experience = Experience.getInstance();
    this.sizes = this.experience.sizes;
    this.scene = this.experience.scene;

    this.setInstance();
    this.setControls();

    if (this.experience.debug.isActive) {
      debugCamera(this);
    }
  }

  private setInstance() {
    this.instance = new THREE.PerspectiveCamera(
      35,
      this.sizes.width / this.sizes.height,
      0.1,
      500
    );

    // thank god....
    this.instance.position.x = 0;
    this.instance.position.y = 0;

    this.scene.add(this.instance);
    this.instance.position.z = 10;
  }

  private setControls() {
    this.controls = new OrbitControls(
      this.instance,
      this.experience.targetElement!
    );
    this.controls.enableDamping = true;
  }

  public resize() {
    this.instance.aspect = this.sizes.width / this.sizes.height;
    this.instance.updateProjectionMatrix();
  }

  public update() {
    // this.instance.position.z += 1;
  }
}
