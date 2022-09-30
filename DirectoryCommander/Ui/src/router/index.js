import { createRouter, createWebHistory } from "vue-router";
import CrawlerPage from "../components/CrawlerPage.vue";
import BuilderPage from "../components/BuilderPage.vue";
import TesterPage from "../components/TesterPage.vue";
import LoadingPage from "../components/LoadingPage.vue";

const routes = new Map([
  ["Home Home", "FadeIn"],
  ["Home Builder", "FromLeftToRight"],
  ["Home Tester", "FromLeftToRight"],
  ["Home Publish", "FromLeftToRight"],

  ["Builder Home", "FromRightToLeft"],
  ["Builder Builder", "FadeIn"],
  ["Builder Tester", "FromLeftToRight"],
  ["Builder Publish", "FromLeftToRight"],

  ["Tester Home", "FromRightToLeft"],
  ["Tester Builder", "FromRightToLeft"],
  ["Tester Tester", "FadeIn"],
  ["Tester Publish", "FromLeftToRight"],

  ["Publish Home", "FromRightToLeft"],
  ["Publish Builder", "FromRightToLeft"],
  ["Publish Tester", "FromRightToLeft"],
  ["Publish Publish", "FadeIn"],
]);

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: "/",
      alias: "/Crawler",
      name: "Home",
      component: CrawlerPage,
      meta: { animation: null },
    },
    {
      path: "/Builder",
      name: "Builder",
      component: BuilderPage,
      meta: { animation: null },
    },
    {
      path: "/Tester",
      name: "Tester",
      component: TesterPage,
      meta: { animation: null },
    },
    {
      path: "/Publish",
      name: "Publish",
      component: LoadingPage,
      meta: { animation: null },
    },
  ],
});

router.beforeEach((to, from) => {
  let toRoute = to.name;
  let fromRoute;

  if (typeof from.name === "undefined") {
    fromRoute = to.name;
  } else {
    fromRoute = from.name;
  }

  to.meta.animation = routes.get(`${fromRoute} ${toRoute}`);
});

export default router;
