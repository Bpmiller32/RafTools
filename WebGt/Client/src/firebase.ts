// Import the functions you need from the SDKs you need
import { initializeApp } from "firebase/app";
import { getFirestore } from "firebase/firestore";

// Your web app's Firebase configuration
// For Firebase JS SDK v7.20.0 and later, measurementId is optional
const firebaseConfig = {
  apiKey: "AIzaSyAp1ts6tNLLnsnR--1Uk9eetdfJAjmT3TU",
  authDomain: "webglgt.firebaseapp.com",
  projectId: "webglgt",
  storageBucket: "webglgt.appspot.com",
  messagingSenderId: "621470716254",
  appId: "1:621470716254:web:f5bb8e8b5e4d37aa5cffcf",
  measurementId: "G-NVMV8QC867",
};

// Initialize Firebase
const app = initializeApp(firebaseConfig);

// Initialize Firestore
const db = getFirestore(app);

export { db };
