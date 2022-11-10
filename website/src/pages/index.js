import React from 'react';
import clsx from 'clsx';
import Link from '@docusaurus/Link';
import CodeBlock from '@theme/CodeBlock';
import Head from '@docusaurus/Head';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import HomepageFeatures from '@site/src/components/HomepageFeatures';

import styles from './index.module.css';

const textContent = {
  intro: `
  {
    "Description": "MLPerf GPU Performance Workload",
    "Parameters": {
      "CudaVersion": "11.6",
      "DriverVersion": "510"
    },
    "Actions": [
      {
        "Type": "MLPerfExecutor",
        "Parameters": {
          "Scenario": "bert"
        }
      },
      {
        "Type": "MLPerfExecutor",
        "Parameters": {
          "Scenario": "rnnt"
        }
      }
    ],
  "Dependencies": [
    {
      "Type": "GitRepoClone"
    },
    {
      "Type": "CudaAndNvidiaGPUDriverInstallation"
    },
    {
      "Type": "DockerInstallation"
    },
    {
      "Type": "NvidiaContainerToolkitInstallation"
    }
  ]
}
  `
}

export function Section({
  element = 'section',
  children,
  className,
  background = 'light',
}) {
  const El = element;
  return (
    <El
      className={
        className
          ? `Section ${className} ${background}`
          : `Section ${background}`
      }>
      {children}
    </El>
  );
}


function HomepageHeader() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <header className={clsx('hero hero--primary', styles.heroBanner)}>
      <div className="container">
        <h1 className="hero__title">{siteConfig.title}</h1>
        <p className="hero__subtitle">{siteConfig.tagline}</p>
        <div className={styles.buttons}>
          <Link
            className="button button--secondary button--lg"
            to="/docs/guides/getting-started">
            Get Started
          </Link>
        </div>
      </div>
    </header>
  );
}

function TwoColumns({columnOne, columnTwo, reverse}) {
  return (
    <div className={`TwoColumns ${reverse ? 'reverse' : ''}`}>
      <div className={`column first ${reverse ? 'right' : 'left'}`}>
        {columnOne}
      </div>
      <div className={`column last ${reverse ? 'left' : 'right'}`}>
        {columnTwo}
      </div>
    </div>
  );
}

function Heading({text}) {
  return <h2 className="Heading">{text}</h2>;
}

function TextColumn({title, text, moreContent}) {
  return (
    <>
      <Heading text={title} />
      <div dangerouslySetInnerHTML={{__html: text}} />
      {moreContent}
    </>
  );
}

function NativeCode() {
  return (
    <Section className="NativeCode" background="tint">
      <TwoColumns
        columnOne={
          <TextColumn
            title="Performance testing, with ease"
            text="Driver and compiler installation are confusing? We got you. <br><br/>
            Virtual Client componentize vaiours dependencies for a benchmark, which you can define in a json flow. <br><br/>
            Performance experts curate those profiles so that you don't have to learn every intricacies of every benchmark."
          />
        }
        columnTwo={
          <CodeBlock language="json">{textContent.intro}</CodeBlock>
        }
      />
    </Section>
  );
}


export default function Home() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout
      title={`Hello from ${siteConfig.title}`}
      description="Benchmarking and system monitoring automation, from MSFT.">
      <HomepageHeader />
      <main>
        <HomepageFeatures />
        
      </main>
      <NativeCode/>
    </Layout>
  );
}