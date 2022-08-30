import { createRouter, createWebHistory } from "vue-router";
import CrawlerPage from "../components/CrawlerPage.vue";
import BuilderPage from "../components/BuilderPage.vue";
import TesterPage from "../components/TesterPage.vue";

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: "/",
      alias: "/Crawler",
      name: "Home",
      component: CrawlerPage,
      meta: { fromRoute: null, toRoute: null },
    },
    {
      path: "/Builder",
      name: "Builder",
      component: BuilderPage,
      meta: { fromRoute: null, toRoute: null },
    },
    {
      path: "/Tester",
      name: "Tester",
      component: TesterPage,
      meta: { fromRoute: null, toRoute: null },
    },
  ],
});

router.beforeEach((to, from) => {
  to.meta.toRoute = to.name;
  if (typeof from.name === "undefined") {
    to.meta.fromRoute = to.name;
  } else {
    to.meta.fromRoute = from.name;
  }
});

export default router;
