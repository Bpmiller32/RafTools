/* -------------------------------------------------------------------------- */
/*          Used to centralize all asset loading in a dedicated class         */
/* -------------------------------------------------------------------------- */

import * as THREE from "three";
import EventEmitter from "./eventEmitter";
import EventMap from "./types/eventMap";

export default class ResourceLoader extends EventEmitter<EventMap> {
  public items: { [key: string]: any };

  private textureLoader?: THREE.TextureLoader;

  constructor() {
    super();

    this.items = {};
    this.textureLoader = new THREE.TextureLoader();
  }

  public loadFromApi(imageUrl?: string) {
    this.textureLoader?.load(imageUrl!, (texture) => {
      this.items["apiImage"] = texture;
      this.emit("loadedFromApi");
    });
  }
}
