/* -------------------------------------------------------------------------- */
/*     Overall handler that mounts a webgl render to a dom canvas element     */
/* -------------------------------------------------------------------------- */

import * as THREE from "three";
import Sizes from "./utils/sizes";
import Time from "./utils/time";
import ResourceLoader from "./utils/resourceLoader";
import Camera from "./camera";
import Renderer from "./renderer";
import World from "./environment/world";
import Debug from "./utils/debug";
import Input from "./utils/input";

export default class Experience {
  // Class prop instance and "new" blocking constructor for Singleton
  private static instance: Experience;
  private constructor() {}

  public debug!: Debug;
  public sizes!: Sizes;
  public time!: Time;
  public input!: Input;
  public resources!: ResourceLoader;

  public targetElement!: HTMLCanvasElement | null;

  public scene!: THREE.Scene;
  public camera!: Camera;
  public renderer!: Renderer;
  public world!: World;

  // Singleton check/constructor
  public static getInstance(): Experience {
    if (!Experience.instance) {
      Experience.instance = new Experience();
    }
    return Experience.instance;
  }

  // Replacement public constructor
  public async configure(canvas: HTMLCanvasElement | null) {
    this.debug = new Debug();
    this.sizes = new Sizes();
    this.time = new Time();
    this.input = new Input();
    this.resources = new ResourceLoader([
      { name: "test", type: "texture", path: "/sample.png" },
    ]);

    this.targetElement = canvas;

    this.scene = new THREE.Scene();
    this.camera = new Camera();
    this.renderer = new Renderer();
    this.world = new World();

    // Sizes resize event
    this.sizes.on("resize", () => {
      this.resize();
    });

    // Time tick event
    this.time.on("tick", () => {
      this.update();
    });
  }

  public resize() {
    this.camera.resize();
    this.renderer.resize();
  }

  public update() {
    if (this.debug.isActive) {
      this.debug.stats?.begin();
    }

    this.camera.update();
    this.world.update();
    this.renderer.update();

    if (this.debug.isActive) {
      this.debug.stats?.end();
    }
  }

  public destroy() {
    // Event listeners
    this.sizes.destroy();
    this.time.destroy();

    // Scene items first
    this.world.destroy();

    // Camera then renderer
    this.renderer.destroy();

    // Debug menu
    if (this.debug.isActive) {
      this.debug.destroy();
    }
  }
}
