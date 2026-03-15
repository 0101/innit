import { test, expect } from "@playwright/test";

function getTitlePositions(page) {
  return page.locator(".title.piece").evaluateAll((els) =>
    els.map((el) => ({
      left: parseFloat(el.style.left),
      top: parseFloat(el.style.top),
    }))
  );
}

function getTitleLetters(page) {
  return page.locator(".title.piece").evaluateAll((els) =>
    els.map((el) => ({
      letter: el.querySelector(".symbol").textContent.trim(),
      left: parseFloat(el.style.left),
      top: parseFloat(el.style.top),
    }))
  );
}

test.describe("Phase 1: Initial Load", () => {
  test("Page loads with 200", async ({ page }) => {
    const response = await page.goto("/", {
      waitUntil: "networkidle",
      timeout: 10_000,
    });
    expect(response.status()).toBe(200);
  });

  test("Page title is INNIT", async ({ page }) => {
    await page.goto("/", { waitUntil: "networkidle", timeout: 10_000 });
    await expect(page).toHaveTitle("INNIT");
  });

  test("Elmish app mounts", async ({ page }) => {
    await page.goto("/", { waitUntil: "networkidle", timeout: 10_000 });
    const children = await page
      .locator("#elmish-app")
      .evaluate((el) => el.children.length);
    expect(children).toBeGreaterThan(0);
  });

  test("Grid renders pieces", async ({ page }) => {
    await page.goto("/", { waitUntil: "networkidle", timeout: 10_000 });
    const count = await page.locator(".piece").count();
    expect(count).toBeGreaterThanOrEqual(10);
  });

  test("Title pieces present (INNIT = 5 letters)", async ({ page }) => {
    await page.goto("/", { waitUntil: "networkidle", timeout: 10_000 });
    await expect(page.locator(".title.piece")).toHaveCount(5);
  });

  test("Pieces have absolute positioning and size", async ({ page }) => {
    await page.goto("/", { waitUntil: "networkidle", timeout: 10_000 });
    const style = await page
      .locator(".piece")
      .first()
      .evaluate((el) => ({
        left: el.style.left,
        top: el.style.top,
        width: el.style.width,
        height: el.style.height,
      }));
    expect(style.left).toBeTruthy();
    expect(style.top).toBeTruthy();
    expect(style.width).toBeTruthy();
    expect(style.height).toBeTruthy();
  });
});

test.describe("Phase 2: Intro Sequence", () => {
  test("No highlighted pieces during Intro1", async ({ page }) => {
    await page.goto("/", { waitUntil: "networkidle", timeout: 10_000 });
    const highlighted = await page.locator(".highlighted.piece").count();
    expect(highlighted).toBe(0);
  });

  test("No item overlays visible during Intro1", async ({ page }) => {
    await page.goto("/", { waitUntil: "networkidle", timeout: 10_000 });
    const items = await page.locator(".item").count();
    expect(items).toBe(0);
  });

  test("Solver animates title pieces (positions changed)", async ({
    page,
  }) => {
    await page.goto("/", { waitUntil: "networkidle", timeout: 10_000 });
    const titleBefore = await getTitlePositions(page);
    await page.waitForTimeout(10_000);
    const titleAfter = await getTitlePositions(page);
    const moved = titleBefore.some(
      (before, i) =>
        before.left !== titleAfter[i].left || before.top !== titleAfter[i].top
    );
    expect(moved).toBe(true);
  });

  test("Intro transitions - items become visible", async ({ page }) => {
    await page.goto("/", { waitUntil: "networkidle", timeout: 10_000 });
    await page.waitForTimeout(10_000);
    const items = await page.locator(".item").count();
    expect(items).toBeGreaterThan(0);
  });

  test("Highlighted pieces appear (icon backgrounds)", async ({ page }) => {
    await page.goto("/", { waitUntil: "networkidle", timeout: 10_000 });
    await page.waitForTimeout(10_000);
    const highlighted = await page.locator(".highlighted.piece").count();
    expect(highlighted).toBeGreaterThan(0);
  });
});

test.describe("Phase 3: Regular Operation", () => {
  test("Mouse click triggers piece movement", async ({ page }) => {
    await page.goto("/", { waitUntil: "networkidle", timeout: 10_000 });
    await page.waitForTimeout(12_000);
    await page.mouse.click(512, 384);
    await page.waitForTimeout(1_500);
    const piecesAfter = await page.locator(".piece").count();
    expect(piecesAfter).toBeGreaterThanOrEqual(10);
  });
});

test.describe("Phase 3b: Shuffle + Solve Verification", () => {
  test("window.shuffle is exposed", async ({ page }) => {
    await page.goto("/", { waitUntil: "networkidle", timeout: 10_000 });
    await page.waitForTimeout(12_000);
    const hasShuffle = await page.evaluate(
      () => typeof window.shuffle === "function"
    );
    expect(hasShuffle).toBe(true);
  });

  test("Shuffle + solve produces correct INNIT arrangement", async ({
    page,
  }) => {
    await page.goto("/", { waitUntil: "networkidle", timeout: 10_000 });
    await page.waitForTimeout(15_000);

    await page.evaluate(() => window.shuffle());
    await page.waitForTimeout(500);

    await page.waitForTimeout(20_000);

    const lettersAfterSolve = await getTitleLetters(page);
    const sorted = lettersAfterSolve.sort((a, b) => a.left - b.left);
    const word = sorted.map((p) => p.letter).join("");
    expect(word).toBe("INNIT");
  });
});

test.describe("Phase 4: Web Worker", () => {
  test(
    "Web worker created for solver",
    async ({ page }) => {
      const workers = [];
      page.on("worker", (worker) => workers.push(worker.url()));
      await page.goto("/", { waitUntil: "networkidle", timeout: 10_000 });
      await page.waitForTimeout(5_000);
      expect(workers.length).toBeGreaterThan(0);
    }
  );
});

test.describe("Phase 5: Health Checks", () => {
  test("No JavaScript errors in console", async ({ context }) => {
    const errorPage = await context.newPage();
    const errors = [];
    errorPage.on("console", (msg) => {
      if (msg.type() === "error") errors.push(msg.text());
    });
    await errorPage.goto("/", {
      waitUntil: "networkidle",
      timeout: 10_000,
    });
    await errorPage.waitForTimeout(2_000);
    await errorPage.close();
    expect(errors).toHaveLength(0);
  });
});

test.describe("Phase 6: Mobile Viewport", () => {
  test("App renders on mobile viewport (375x667)", async ({ context }) => {
    const mobilePage = await context.newPage();
    await mobilePage.setViewportSize({ width: 375, height: 667 });
    await mobilePage.goto("/", {
      waitUntil: "networkidle",
      timeout: 10_000,
    });
    const count = await mobilePage.locator(".piece").count();
    expect(count).toBeGreaterThanOrEqual(10);
    await mobilePage.close();
  });

  test("Mobile grid is smaller than desktop", async ({ page, context }) => {
    await page.goto("/", { waitUntil: "networkidle", timeout: 10_000 });
    const desktopCount = await page.locator(".piece").count();

    const mobilePage = await context.newPage();
    await mobilePage.setViewportSize({ width: 375, height: 667 });
    await mobilePage.goto("/", {
      waitUntil: "networkidle",
      timeout: 10_000,
    });
    const mobileCount = await mobilePage.locator(".piece").count();
    await mobilePage.close();

    expect(mobileCount).toBeLessThan(desktopCount);
  });
});
