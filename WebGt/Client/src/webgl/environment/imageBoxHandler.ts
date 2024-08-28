/* -------------------------------------------------------------------------- */
/*   Handler for creating and joining clipping boxes, cropping to image box   */
/* -------------------------------------------------------------------------- */

import * as THREE from "three";
import Experience from "../experience";
import Camera from "../camera";
import Input from "../utils/input";
import Sizes from "../utils/sizes";
import ResourceLoader from "../utils/resourceLoader";
import Renderer from "../renderer";

export default class ImageBoxHandler {
  private experience: Experience;
  private renderer: Renderer;
  private resources: ResourceLoader;
  private scene: THREE.Scene;
  private camera: Camera;
  private sizes: Sizes;
  private input: Input;

  public geometry!: THREE.BoxGeometry;
  public materials!: THREE.MeshBasicMaterial[];
  public mesh!: THREE.Mesh;

  constructor() {
    // Experience fields
    this.experience = Experience.getInstance();
    this.renderer = this.experience.renderer;
    this.resources = this.experience.resources;
    this.scene = this.experience.scene;
    this.camera = this.experience.camera;
    this.sizes = this.experience.sizes;
    this.input = this.experience.input;

    // Class fields
    this.setGeometry();
    this.setMaterial();
    this.setMesh();

    // Events
    this.input.on("screenshotImage", () => {
      this.screenshotImage();
    });
    this.input.on("resetImage", () => {
      this.resetImage();
    });
  }
  /* ---------------------------- Instance methods ---------------------------- */
  private setGeometry() {
    this.geometry = new THREE.BoxGeometry(5, 5, 1);
  }

  private setMaterial() {
    this.materials = [
      new THREE.MeshBasicMaterial({ color: 0x00ff00 }), // Right face
      new THREE.MeshBasicMaterial({ color: 0xff0000 }), // Left face
      new THREE.MeshBasicMaterial({ color: 0x0000ff }), // Top face
      new THREE.MeshBasicMaterial({ color: 0xffff00 }), // Bottom face
      new THREE.MeshBasicMaterial({ map: this.resources.items.test }), // Front face with texture
      new THREE.MeshBasicMaterial({ color: 0xffffff }), // Back face
    ];
  }

  private setMesh() {
    this.mesh = new THREE.Mesh(this.geometry, this.materials);
    this.scene.add(this.mesh);

    // Update matrix updates local transform, otherwise mesh is still at 0
    this.mesh.updateMatrix();
  }

  /* ------------------------------ Event methods ----------------------------- */
  private screenshotImage() {
    // Store the camera's current position and zoom level, render's resolution
    const originalPosition = this.camera.orthographicCamera.position;
    const originalZoom = this.camera.orthographicCamera.zoom;
    const originalPositionTarget = this.camera.cameraPositionTarget;
    const originalZoomTarget = this.camera.zoomTarget;
    const originalRendererSize = new THREE.Vector2();
    this.renderer.instance.getSize(originalRendererSize);

    // Compute the bounding box of the mesh
    const boundingBox = new THREE.Box3().setFromObject(this.mesh);
    const boundingBoxSize = boundingBox.getSize(new THREE.Vector3());
    const boundingBoxCenter = boundingBox.getCenter(new THREE.Vector3());

    // Debug, create a BoxHelper to visualize the bounding box
    // const boxHelper = new THREE.BoxHelper(this.mesh, 0xffff00); // Yellow lines
    // this.scene.add(boxHelper);

    // Adjust the camera to fit the bounding box
    const maxDim = Math.max(
      boundingBoxSize.x,
      boundingBoxSize.y,
      boundingBoxSize.z
    );
    this.camera.orthographicCamera.position.set(
      boundingBoxCenter.x,
      boundingBoxCenter.y,
      boundingBoxCenter.z + maxDim
    ); // Adjust z as necessary
    this.camera.orthographicCamera.zoom = Math.min(
      this.camera.orthographicCamera.right / boundingBoxSize.x,
      this.camera.orthographicCamera.top / boundingBoxSize.y
    );
    this.camera.orthographicCamera.updateProjectionMatrix();

    // Calculate screenshot width and height based on renderer aspect ratio
    const desiredHeight = 1080;
    const desiredWidth = (desiredHeight * this.sizes.width) / this.sizes.height;

    // Render and take the screenshot
    this.renderer.instance.setSize(desiredWidth, desiredHeight); // Set the desired resolution
    this.renderer.instance.render(this.scene, this.camera.orthographicCamera);

    // Screenshot in base64
    const dataUrl = this.renderer.instance.domElement.toDataURL();

    // Automatically download the screenshot as a PNG file
    const link = document.createElement("a");
    link.href = dataUrl;
    link.download = "screenshot.png"; // Specify the file name
    link.click();
    // document.removeChild(link);

    // Restore the original position and zoom level, renderer size
    this.camera.orthographicCamera.position.copy(originalPosition);
    this.camera.orthographicCamera.zoom = originalZoom;
    this.camera.cameraPositionTarget = originalPositionTarget;
    this.camera.zoomTarget = originalZoomTarget;
    this.camera.orthographicCamera.updateProjectionMatrix();

    this.renderer.instance.setSize(
      originalRendererSize.width,
      originalRendererSize.height
    );
  }

  private resetImage() {
    this.scene.remove(this.mesh);
    this.mesh = new THREE.Mesh(this.geometry, this.materials);
    this.scene.add(this.mesh);

    this.camera.orthographicCamera.position.set(0, 0, 10);
    this.camera.cameraPositionTarget.set(0, 0, 10);
    this.camera.orthographicCamera.zoom = 1;
    this.camera.zoomTarget = 1;
  }

  /* ------------------------------ Tick methods ------------------------------ */
  public destroy() {}
}
