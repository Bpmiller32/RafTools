import { createApp, watch } from "vue";
import App from "./App.vue";
import { createPinia } from "pinia";
import router from "./router";
import "./assets/tailwind.css";

// // ** Pinia entire state persistant without plugin
// const pinia = createPinia();
// // On new route, grab state if it exists in storage and set it to pinia's state
// if (sessionStorage.getItem("state")) {
//   pinia.state.value = JSON.parse(sessionStorage.getItem("state"));
// }
// // Deep watcher on pinia's entire state, set storage on change
// watch(
//   pinia.state,
//   (state) => {
//     sessionStorage.setItem("state", JSON.stringify(state));
//   },
//   { deep: true }
// );

// Pinia persistant state with plugin
createApp(App).use(createPinia()).use(router).mount("#app");

// Idea to not use a router at all...
// createApp(App).use(createPinia()).mount("#app");
