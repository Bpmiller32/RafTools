import { createRouter, createWebHistory } from "vue-router";
import HelloWorld from "../components/HelloWorld.vue";
import CrawlerCard from "../components/CrawlerCard.vue";

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: "/", name: "Home", component: CrawlerCard, alias: "/Crawler" },
    // { path: "/Crawler", name: "Crawler", component: CrawlerCard },
    { path: "/Builder", name: "Builder", component: HelloWorld },
    { path: "/Tester", name: "Tester", component: HelloWorld },
  ],
});

export default router;
