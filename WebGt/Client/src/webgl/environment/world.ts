/* -------------------------------------------------------------------------- */
/*         The "World" in which all resources for the webgl scene live        */
/* -------------------------------------------------------------------------- */

import * as THREE from "three";
import Experience from "../experience";
import ResourceLoader from "../utils/resourceLoader.ts";
import Camera from "../camera.ts";
import { CSG } from "three-csg-ts";
import Input from "../utils/input.ts";
import CroppingBox from "./croppingBox.ts";
import Debug from "../utils/debug.ts";

export default class World {
  private experience: Experience;
  private resources: ResourceLoader;
  private camera: Camera;
  private scene: THREE.Scene;
  private input: Input;
  private debug!: Debug;

  constructor() {
    this.experience = Experience.getInstance();
    this.resources = this.experience.resources;
    this.camera = this.experience.camera;
    this.scene = this.experience.scene;
    this.input = this.experience.input;

    let croppingBoxes: CroppingBox[] = [];

    // Debug GUI
    if (this.experience.debug.isActive) {
      this.debug = this.experience.debug;

      const worldDebug = this.debug.ui?.addFolder("worldDebug");
      worldDebug?.open();
      worldDebug
        ?.add(croppingBoxes, "length")
        .name("# of croppingBoxes")
        .listen();
      worldDebug
        ?.add(this.scene.children, "length")
        .name("# of scene children")
        .listen();
    }

    let spriteBoxMesh: THREE.Mesh;

    // Resources events
    this.resources.on("ready", () => {
      // Set material
      const spriteBoxMaterials = [
        new THREE.MeshBasicMaterial({ color: 0x00ff00 }), // Right face
        new THREE.MeshBasicMaterial({ color: 0xff0000 }), // Left face
        new THREE.MeshBasicMaterial({ color: 0x0000ff }), // Top face
        new THREE.MeshBasicMaterial({ color: 0xffff00 }), // Bottom face
        new THREE.MeshBasicMaterial({ map: this.resources.items.test }), // Front face with texture
        new THREE.MeshBasicMaterial({ color: 0xffffff }), // Back face
      ];

      // Set geometry
      const spriteBoxGeometry = new THREE.BoxGeometry(5, 5, 5, 10, 10, 10);

      // Set "mesh"
      spriteBoxMesh = new THREE.Mesh(spriteBoxGeometry, spriteBoxMaterials);
      spriteBoxMesh.name = "gtImage";

      // update matrix, updates local transform, otherwise mesh is still at 0
      spriteBoxMesh.updateMatrix();

      // Add to scene
      this.experience.scene.add(spriteBoxMesh);
    });

    // Input events
    this.input.on("mouseDown", () => {
      croppingBoxes.push(
        new CroppingBox(this.input.clickStartPoint, this.input.clickEndPoint)
      );
    });
    this.input.on("mouseMove", () => {
      if (croppingBoxes.length !== 0) {
        const lastCroppingBox = croppingBoxes[croppingBoxes.length - 1];
        lastCroppingBox.updateSize();
      }
    });
    this.input.on("mouseUp", () => {
      if (croppingBoxes.length !== 0) {
        const lastCroppingBox = croppingBoxes[croppingBoxes.length - 1];
        console.log("grabbing last box:", lastCroppingBox);
        lastCroppingBox.castRay();
      }
    });
    this.input.on("stitchBoxes", () => {
      if (croppingBoxes.length !== 0) {
        // Loop through the rest of the meshes and union them, discard the original mesh
        let combinedMesh = croppingBoxes[0].mesh;

        for (let i = 1; i < croppingBoxes.length; i++) {
          combinedMesh = CSG.union(combinedMesh, croppingBoxes[i].mesh);
          croppingBoxes[i].destroy();
        }

        // Dispose the existing croppingBoxes
        croppingBoxes[0].destroy();
        croppingBoxes.length = 0;

        combinedMesh.scale.z += 10;
        const croppedImage = CSG.intersect(spriteBoxMesh, combinedMesh);
        this.scene.remove(spriteBoxMesh);
        this.scene.add(croppedImage);

        croppedImage.name = "gtImage";
        spriteBoxMesh = croppedImage;
      }
    });
    this.input.on("screenshotImage", () => {
      const boundingBox = new THREE.Box3().setFromObject(spriteBoxMesh);
      const center = boundingBox.getCenter(new THREE.Vector3());

      this.camera.instance.lookAt(center);

      const size = boundingBox.getSize(new THREE.Vector3());
      const distance = size.length();
      const direction = new THREE.Vector3()
        .subVectors(this.camera.instance.position, center)
        .normalize();

      this.camera.instance.position
        .copy(center)
        .add(direction.multiplyScalar(distance));
      this.camera.instance.updateProjectionMatrix();
    });
  }

  public update() {
    this.camera.update();
  }

  public destroy() {}
}
