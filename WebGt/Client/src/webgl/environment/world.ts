/* -------------------------------------------------------------------------- */
/*         The "World" in which all resources for the webgl scene live        */
/* -------------------------------------------------------------------------- */

import * as THREE from "three";
import Experience from "../experience";
import ResourceLoader from "../utils/resourceLoader.ts";
import Camera from "../camera.ts";
import Debug from "../utils/debug.ts";
import ClipBoxHandler from "./clipBoxHandler.ts";
import ImageBoxHandler from "./imageBoxHandler.ts";

export default class World {
  private experience: Experience;
  private resources: ResourceLoader;
  private camera: Camera;
  private scene: THREE.Scene;
  private debug!: Debug;

  public spriteBoxMesh!: THREE.Mesh;
  public imageBoxHandler!: ImageBoxHandler;
  public clipBoxHandler!: ClipBoxHandler;

  constructor() {
    // Experience fields
    this.experience = Experience.getInstance();
    this.resources = this.experience.resources;
    this.camera = this.experience.camera;
    this.scene = this.experience.scene;

    // Class fields

    // Debug GUI
    if (this.experience.debug.isActive) {
      this.debug = this.experience.debug;

      const worldDebug = this.debug.ui?.addFolder("worldDebug");
      worldDebug?.open();
      worldDebug
        ?.add(this.scene.children, "length")
        .name("# of scene children")
        .listen();
      worldDebug
        ?.add(this.experience.sizes, "width")
        .name("renderer width")
        .listen();
      worldDebug
        ?.add(this.experience.sizes, "height")
        .name("renderer height")
        .listen();
    }

    // Resources events
    this.resources.on("ready", () => {
      this.imageBoxHandler = new ImageBoxHandler();
      this.clipBoxHandler = new ClipBoxHandler();
    });
  }

  public update() {
    this.camera.update();
  }

  public destroy() {
    this.camera.destroy();
    this.imageBoxHandler.destroy();
    this.clipBoxHandler.destroy();
  }
}
