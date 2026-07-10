import { getDashboardStats } from "@/lib/services/stats-service";
import { DashboardClient } from "@/components/dashboard/dashboard-client";

export const dynamic = "force-dynamic";

export default async function DashboardPage() {
  const stats = await getDashboardStats();
  // Serialize Dates to plain JSON before crossing the server/client boundary.
  const serializable = JSON.parse(JSON.stringify(stats));
  return <DashboardClient stats={serializable} />;
}
