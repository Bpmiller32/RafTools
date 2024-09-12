/* -------------------------------------------------------------------------- */
/*          Used to centralize all asset loading in a dedicated class         */
/* -------------------------------------------------------------------------- */

import Emitter from "./eventEmitter";
import * as THREE from "three";

export default class ResourceLoader {
  public items: { [key: string]: any };

  private textureLoader?: THREE.TextureLoader;

  constructor() {
    this.items = {};
    this.textureLoader = new THREE.TextureLoader();
  }

  public loadFromApi(imageUrl?: string) {
    this.textureLoader?.load(imageUrl!, (texture) => {
      this.items["apiImage"] = texture;
      Emitter.emit("loadedFromApi");
    });
  }
}
