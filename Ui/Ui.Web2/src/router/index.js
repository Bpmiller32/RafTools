import { createRouter, createWebHistory, useRoute } from "vue-router";
import HelloWorld from "../components/HelloWorld.vue";
import CrawlerPage from "../components/CrawlerPage.vue";
import { useStore } from "../store";

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: "/",
      name: "Home",
      component: CrawlerPage,
      alias: "/Crawler",
    },
    // { path: "/Crawler", name: "Crawler", component: CrawlerCard },
    {
      path: "/Builder",
      name: "Builder",
      component: HelloWorld,
    },
    {
      path: "/Tester",
      name: "Tester",
      component: HelloWorld,
    },
  ],
});

router.beforeEach((to) => {
  const store = useStore();
  store.prevRoute = store.currRoute;
});

router.afterEach((to) => {
  const store = useStore();
  store.currRoute = to.name;
});

export default router;
