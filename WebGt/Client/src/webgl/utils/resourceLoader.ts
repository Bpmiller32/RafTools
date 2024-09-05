/* -------------------------------------------------------------------------- */
/*          Used to centralize all asset loading in a dedicated class         */
/* -------------------------------------------------------------------------- */

import * as THREE from "three";
// import { DRACOLoader, GLTFLoader } from "three/examples/jsm/Addons.js";
import EventEmitter from "./eventEmitter";
import EventMap from "./types/eventMap";
import Resource from "./types/resource";

export default class ResourceLoader extends EventEmitter<EventMap> {
  private sources!: Resource[];
  public items!: { [key: string]: any };
  public toLoad!: number;
  public loaded!: number;

  // private gltfLoader?: GLTFLoader;
  // private dracoLoader?: DRACOLoader;
  private textureLoader?: THREE.TextureLoader;

  constructor(sources?: Resource[]) {
    super();

    if (sources) {
      this.sources = sources;
      this.items = {};
      this.toLoad = this.sources.length;
      this.loaded = 0;

      this.setLoaders();
      this.startLoadingFromLocal();
    } else {
      this.setLoaders();
      this.startLoadingFromApi();
    }
  }

  private setLoaders() {
    // const dracoLoader = new DRACOLoader();
    // dracoLoader.setDecoderPath("/draco/");

    // this.gltfLoader = new GLTFLoader();
    // this.gltfLoader.setDRACOLoader(dracoLoader);

    this.textureLoader = new THREE.TextureLoader();
  }

  private startLoadingFromLocal() {
    for (const source of this.sources) {
      switch (source.type) {
        // case "gltfModel":
        //   this.gltfLoader?.load(source.path, (file) => {
        //     this.sourceLoaded(source, file);
        //   });
        //   break;

        case "texture":
          this.textureLoader?.load(source.path, (file) => {
            this.sourceLoaded(source, file);
          });
          break;

        default:
          break;
      }
    }
  }

  private startLoadingFromApi() {}

  private sourceLoaded(source: Resource, file: any) {
    this.items[source.name] = file;
    this.loaded++;

    if (this.loaded === this.toLoad) {
      this.emit("ready");
    }
  }
}
