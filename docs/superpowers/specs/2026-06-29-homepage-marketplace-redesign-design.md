# Homepage Marketplace Redesign Design

## Goal

Redesign the current home page into a denser ecommerce marketplace-style page while keeping the first implementation view-only. The page should feel closer to a Vietnamese marketplace homepage: category browsing, promotion-heavy hero, quick shopping shortcuts, sale products, featured products, and existing trust sections.

## Scope

In scope:

- Redesign `Views/Home/Index.cshtml`.
- Reuse existing data from `ViewBag.AllProducts`, `ViewBag.Categories`, `ViewBag.Testimonials`, and `FeaturesBannerViewComponent`.
- Reuse existing product card and home partials where they still fit.
- Keep the current navbar, footer, login modal, cart form behavior, and search route.
- Use Fruitables green as the primary color, with orange/red accents for sale and deal areas.

Out of scope:

- No new database tables, migrations, or voucher/deal models.
- No new dependencies.
- No backend service redesign.
- No navbar/footer redesign unless required by layout breakage.

## Recommended Approach

Use a view-only redesign. This keeps the change small and reversible while producing the requested marketplace feel. The implementation should avoid new abstractions unless the Razor file becomes hard to read; if a tiny repeated block is needed once or twice, inline it.

## Page Structure

### Marketplace Hero

The first viewport should become a three-column marketplace layout:

- Left column: category menu from `ViewBag.Categories`, capped to 8 parent categories, plus a fallback link to the shop.
- Center column: large promotional hero with a real product/fruit image from existing assets, search input, and clear calls to action.
- Right column: compact trust/deal cards such as freeship, daily deal, and support. These are static marketing blocks because there is no voucher/deal domain model yet.

### Quick Actions

Add a strip of marketplace shortcuts below the hero:

- Flash sale
- Best sellers
- Fresh vegetables
- Fruits
- Saving combos

These should be plain links or anchors. They should not require new JavaScript logic.

### Product Sections

Add lightweight product sections before the existing category tab grid:

- `Giá tốt hôm nay`: products from `AllProducts` where `SalePrice` is lower than `Price`.
- `Sản phẩm nổi bật`: products from `AllProducts` where `IsFeatured` is true.
- Existing `_ProductsSection`: keep as the main category browsing grid.
- Existing banner, best seller, and testimonial sections: keep, but place after the stronger shopping sections.

If an extracted existing partial expects a `ViewBag` value that the controller does not currently set, implementation should either set the value in the view from `AllProducts` or skip that partial when empty. Do not add backend just for this redesign.

## Fallback Behavior

- If there are no categories, show a simple shop link instead of an empty category panel.
- If there are no sale products, hide the sale section.
- If there are no featured products, hide the featured section.
- If there are no products at all, keep the existing empty product messaging from `_ProductsSection`.
- Product images should continue using the existing `_ProductCard` fallback behavior.

## Interaction

- Search remains a GET form to `Shop/Index` with the existing `search` query parameter.
- Add-to-cart forms remain unchanged so the existing login interception and cart behavior continue to work.
- Category links should use existing shop navigation patterns. If the current shop route does not support category query strings cleanly, use shop links without new routing work.
- Avoid new JavaScript unless it replaces an existing inline behavior with less code.

## Visual Direction

- Keep the Fruitables green identity.
- Use orange/red only for promotion, sale, and deal emphasis.
- Use denser ecommerce spacing than the current landing-style hero.
- Keep cards readable on mobile; the hero columns should stack in a useful order: search/deal hero first, quick actions second, category list after if space is tight.

## Verification

- Run `dotnet test`.
- Run the app and inspect the homepage on desktop and mobile widths.
- Confirm search submits to shop.
- Confirm add-to-cart still triggers the existing authenticated/unauthenticated behavior.
- Confirm empty sale/featured/category states do not leave blank sections.
